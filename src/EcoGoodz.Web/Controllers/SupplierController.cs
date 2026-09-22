using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Supplier;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class SupplierController : PagedListController<Data.Models.Supplier, SupplierListItemViewModel>
{
    public SupplierController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.Supplier> GetBaseQuery() =>
        Context.Suppliers.Include(s => s.AccountManagerNavigation);

    protected override IQueryable<Data.Models.Supplier> ApplySearch(IQueryable<Data.Models.Supplier> query, string searchTerm) =>
        query.Where(s =>
            (s.Name != null && s.Name.Contains(searchTerm))
            || (s.AccountManagerNavigation != null && (
                (s.AccountManagerNavigation.FirstName != null && s.AccountManagerNavigation.FirstName.Contains(searchTerm))
                || (s.AccountManagerNavigation.LastName != null && s.AccountManagerNavigation.LastName.Contains(searchTerm)))));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.Supplier, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.Supplier, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = s => EF.Property<string>(s, "NameSort"),
            ["accountManager"] = s => s.AccountManagerNavigation != null ? s.AccountManagerNavigation.FirstName : null,
            ["active"] = s => s.IsActive,
        };

    protected override string DefaultSortColumn => "name";

    protected override Expression<Func<Data.Models.Supplier, SupplierListItemViewModel>> ProjectionExpression =>
        s => new SupplierListItemViewModel
        {
            Id = s.Id,
            Name = s.Name ?? string.Empty,
            AccountManagerName = s.AccountManagerNavigation != null
                ? s.AccountManagerNavigation.FirstName + " " + s.AccountManagerNavigation.LastName
                : null,
            IsActive = s.IsActive ?? false,
        };

    public async Task<IActionResult> Details(int id)
    {
        var supplier = await Context.Suppliers
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

        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await Context.Suppliers.FindAsync(id);
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

        var supplier = await Context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.Name = model.Name;
        supplier.AccountManager = model.AccountManager;
        supplier.IsActive = model.IsActive;
        supplier.UpdatedOn = DateTime.UtcNow;
        supplier.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var supplier = await Context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.IsActive = false;
        supplier.UpdatedOn = DateTime.UtcNow;
        supplier.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetAccountManagerOptionsAsync()
    {
        return await Context.Users
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
