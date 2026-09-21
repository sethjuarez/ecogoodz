using EcoGoodz.Data;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Supplier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class SupplierController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public SupplierController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.AccountManagerNavigation)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierListItemViewModel
            {
                Id = s.Id,
                Name = s.Name ?? string.Empty,
                AccountManagerName = s.AccountManagerNavigation != null
                    ? s.AccountManagerNavigation.FirstName + " " + s.AccountManagerNavigation.LastName
                    : null,
                IsActive = s.IsActive ?? false,
            })
            .ToListAsync();

        return View(suppliers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.AccountManagerNavigation)
            .Where(s => s.Id == id)
            .Select(s => new SupplierDetailsViewModel
            {
                Id = s.Id,
                Name = s.Name ?? string.Empty,
                AccountManagerName = s.AccountManagerNavigation != null
                    ? s.AccountManagerNavigation.FirstName + " " + s.AccountManagerNavigation.LastName
                    : null,
                IsActive = s.IsActive ?? false,
                CreateOn = s.CreateOn,
                UpdatedOn = s.UpdatedOn,
                ProductCount = s.SupplierProducts.Count,
                BuyerCount = s.BuyerSuppliers.Count,
                LoadCount = s.Loads.Count,
            })
            .FirstOrDefaultAsync();

        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    public async Task<IActionResult> Create()
    {
        var model = new SupplierFormViewModel { AccountManagerOptions = await GetAccountManagerOptionsAsync() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AccountManagerOptions = await GetAccountManagerOptionsAsync();
            return View(model);
        }

        var supplier = new Data.Models.Supplier
        {
            Name = model.Name,
            AccountManager = model.AccountManager,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        var model = new SupplierFormViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name ?? string.Empty,
            AccountManager = supplier.AccountManager,
            IsActive = supplier.IsActive ?? false,
            AccountManagerOptions = await GetAccountManagerOptionsAsync(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model)
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

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.Name = model.Name;
        supplier.AccountManager = model.AccountManager;
        supplier.IsActive = model.IsActive;
        supplier.UpdatedOn = DateTime.UtcNow;
        supplier.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.IsActive = false;
        supplier.UpdatedOn = DateTime.UtcNow;
        supplier.UpdatedBy = User.GetLegacyUserId();

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
