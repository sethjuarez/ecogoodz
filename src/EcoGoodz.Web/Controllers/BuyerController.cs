using EcoGoodz.Data;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Buyer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class BuyerController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public BuyerController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var buyers = await _context.Buyers
            .Include(b => b.AccountManagerNavigation)
            .OrderBy(b => b.Name)
            .Select(b => new BuyerListItemViewModel
            {
                Id = b.Id,
                Name = b.Name ?? string.Empty,
                AccountManagerName = b.AccountManagerNavigation != null
                    ? b.AccountManagerNavigation.FirstName + " " + b.AccountManagerNavigation.LastName
                    : null,
                IsActive = b.IsActive ?? false,
            })
            .ToListAsync();

        return View(buyers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var buyer = await _context.Buyers
            .Include(b => b.AccountManagerNavigation)
            .Where(b => b.Id == id)
            .Select(b => new BuyerDetailsViewModel
            {
                Id = b.Id,
                Name = b.Name ?? string.Empty,
                AccountManagerName = b.AccountManagerNavigation != null
                    ? b.AccountManagerNavigation.FirstName + " " + b.AccountManagerNavigation.LastName
                    : null,
                Note = b.Note,
                IsActive = b.IsActive ?? false,
                CreateOn = b.CreateOn,
                UpdatedOn = b.UpdatedOn,
                ProductCount = b.BuyerProducts.Count,
                SupplierCount = b.BuyerSuppliers.Count,
                LoadCount = b.Loads.Count,
            })
            .FirstOrDefaultAsync();

        if (buyer is null)
        {
            return NotFound();
        }

        return View(buyer);
    }

    public async Task<IActionResult> Create()
    {
        var model = new BuyerFormViewModel { AccountManagerOptions = await GetAccountManagerOptionsAsync() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuyerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AccountManagerOptions = await GetAccountManagerOptionsAsync();
            return View(model);
        }

        var buyer = new Data.Models.Buyer
        {
            Name = model.Name,
            AccountManager = model.AccountManager,
            Note = model.Note,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Buyers.Add(buyer);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var buyer = await _context.Buyers.FindAsync(id);
        if (buyer is null)
        {
            return NotFound();
        }

        var model = new BuyerFormViewModel
        {
            Id = buyer.Id,
            Name = buyer.Name ?? string.Empty,
            AccountManager = buyer.AccountManager,
            Note = buyer.Note,
            IsActive = buyer.IsActive ?? false,
            AccountManagerOptions = await GetAccountManagerOptionsAsync(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuyerFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.AccountManagerOptions = await GetAccountManagerOptionsAsync();
            return View(model);
        }

        var buyer = await _context.Buyers.FindAsync(id);
        if (buyer is null)
        {
            return NotFound();
        }

        buyer.Name = model.Name;
        buyer.AccountManager = model.AccountManager;
        buyer.Note = model.Note;
        buyer.IsActive = model.IsActive;
        buyer.UpdatedOn = DateTime.UtcNow;
        buyer.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Soft delete only, matching the legacy IsActive convention - buyers are
    // referenced by loads/history throughout the schema and are never hard-deleted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var buyer = await _context.Buyers.FindAsync(id);
        if (buyer is null)
        {
            return NotFound();
        }

        buyer.IsActive = false;
        buyer.UpdatedOn = DateTime.UtcNow;
        buyer.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetAccountManagerOptionsAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive == true)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.FirstName + " " + u.LastName,
            })
            .ToListAsync();
    }
}
