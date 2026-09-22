using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.StaffUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class StaffUserController : Controller
{
    private readonly EcoGoodzDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffUserController(EcoGoodzDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(u => u.RoleNavigation)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new StaffUserListItemViewModel
            {
                Id = u.Id,
                Name = ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim(),
                UserName = u.UserName,
                Email = u.Email,
                OfficePhone = u.OfficePhone,
                CellPhone = u.CellPhone,
                RoleName = u.RoleNavigation != null ? u.RoleNavigation.RoleName : null,
                IsActive = u.IsActive == true,
            })
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StaffUserFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffUserFormViewModel model)
    {
        await ValidateUniqueUserAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Initials = model.Initials,
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            OfficePhone = model.OfficePhone,
            CellPhone = model.CellPhone,
            Role = model.Role,
            IsSendCompanyReport = model.IsSendCompanyReport,
            IsSendManagerReport = model.IsSendManagerReport,
            IsShowCommunicationReport = model.IsShowCommunicationReport,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (!await UpsertApplicationUserAsync(user))
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await PopulateOptionsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var model = new StaffUserFormViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName,
            Initials = user.Initials,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            OfficePhone = user.OfficePhone,
            CellPhone = user.CellPhone,
            Role = user.Role,
            IsSendCompanyReport = user.IsSendCompanyReport,
            IsSendManagerReport = user.IsSendManagerReport,
            IsShowCommunicationReport = user.IsShowCommunicationReport,
            IsActive = user.IsActive == true,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffUserFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        await ValidateUniqueUserAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Initials = model.Initials;
        user.UserName = model.UserName.Trim();
        user.Email = model.Email.Trim();
        user.OfficePhone = model.OfficePhone;
        user.CellPhone = model.CellPhone;
        user.Role = model.Role;
        user.IsSendCompanyReport = model.IsSendCompanyReport;
        user.IsSendManagerReport = model.IsSendManagerReport;
        user.IsShowCommunicationReport = model.IsShowCommunicationReport;
        user.IsActive = model.IsActive;
        user.UpdatedOn = DateTime.UtcNow;
        user.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();
        await EnsureDefaultHeadlinesAsync(user.Id);
        if (!await UpsertApplicationUserAsync(user))
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        user.UpdatedOn = DateTime.UtcNow;
        user.UpdatedBy = User.GetLegacyUserId();
        await _context.SaveChangesAsync();
        await SyncIdentityLockoutAsync(user);

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueUserAsync(StaffUserFormViewModel model)
    {
        if (await _context.Users.AnyAsync(u => u.Id != model.Id && u.UserName != null && u.UserName.ToLower() == model.UserName.Trim().ToLower()))
        {
            ModelState.AddModelError(nameof(model.UserName), "Username already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.Id != model.Id && u.Email != null && u.Email.ToLower() == model.Email.Trim().ToLower()))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
        }
    }

    private async Task EnsureDefaultHeadlinesAsync(int userId)
    {
        foreach (var headlineName in new[] { "Assigned To", "Assigned" })
        {
            if (await _context.TaskHeadlines.AnyAsync(h => h.UserId == userId && h.Headline == headlineName))
            {
                continue;
            }

            _context.TaskHeadlines.Add(new TaskHeadline
            {
                UserId = userId,
                Headline = headlineName,
                IsActive = true,
                CreateOn = DateTime.UtcNow,
                CreatedBy = User.GetLegacyUserId() ?? userId,
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<bool> UpsertApplicationUserAsync(User legacyUser)
    {
        var applicationUser = await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUserId == legacyUser.Id);
        var isNew = applicationUser is null;
        applicationUser ??= new ApplicationUser
        {
            LegacyUserId = legacyUser.Id,
            MustChangePassword = true,
            LockoutEnabled = true,
        };

        applicationUser.UserName = legacyUser.UserName;
        applicationUser.Email = legacyUser.Email;
        applicationUser.LockoutEnabled = true;

        var result = isNew
            ? await _userManager.CreateAsync(applicationUser)
            : await _userManager.UpdateAsync(applicationUser);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return false;
        }

        var roleName = legacyUser.RoleNavigation?.RoleName
            ?? await _context.Roles.Where(r => r.Id == legacyUser.Role).Select(r => r.RoleName).FirstOrDefaultAsync();
        await SyncIdentityRoleAsync(applicationUser, roleName);
        await SyncIdentityLockoutAsync(legacyUser, applicationUser);
        await EnsureDefaultHeadlinesAsync(legacyUser.Id);
        return true;
    }

    private async Task SyncIdentityRoleAsync(ApplicationUser applicationUser, string? roleName)
    {
        var currentRoles = await _userManager.GetRolesAsync(applicationUser);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(applicationUser, currentRoles);
        }

        if (!string.IsNullOrWhiteSpace(roleName) && AppRoles.All.Contains(roleName))
        {
            await _userManager.AddToRoleAsync(applicationUser, roleName);
        }
    }

    private async Task SyncIdentityLockoutAsync(User legacyUser, ApplicationUser? applicationUser = null)
    {
        applicationUser ??= await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUserId == legacyUser.Id);
        if (applicationUser is null)
        {
            return;
        }

        if (legacyUser.IsActive == true)
        {
            await _userManager.SetLockoutEndDateAsync(applicationUser, null);
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(applicationUser, true);
            await _userManager.SetLockoutEndDateAsync(applicationUser, DateTimeOffset.UtcNow.AddYears(100));
        }
    }

    private async Task PopulateOptionsAsync(StaffUserFormViewModel model)
    {
        model.RoleOptions = await _context.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.RoleName })
            .ToListAsync();
    }
}
