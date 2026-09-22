using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.PackageType;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class PackageTypeController : PagedListController<Data.Models.PackageType, PackageTypeListItemViewModel>
{
    public PackageTypeController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.PackageType> GetBaseQuery() => Context.PackageTypes;

    protected override IQueryable<Data.Models.PackageType> ApplySearch(IQueryable<Data.Models.PackageType> query, string searchTerm) =>
        query.Where(p => p.Type != null && p.Type.Contains(searchTerm));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.PackageType, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.PackageType, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = p => EF.Property<string>(p, "TypeSort"),
            ["active"] = p => p.IsActive,
        };

    protected override string DefaultSortColumn => "type";

    protected override Expression<Func<Data.Models.PackageType, PackageTypeListItemViewModel>> ProjectionExpression =>
        p => new PackageTypeListItemViewModel
        {
            Id = p.Id,
            Type = p.Type ?? string.Empty,
            IsActive = p.IsActive ?? false,
        };

    public async Task<IActionResult> Details(int id)
    {
        var packageType = await Context.PackageTypes
            .Where(p => p.Id == id)
            .Select(p => new PackageTypeDetailsViewModel
            {
                Id = p.Id,
                Type = p.Type ?? string.Empty,
                IsActive = p.IsActive ?? false,
                CreateOn = p.CreateOn,
                UpdatedOn = p.UpdatedOn,
                BuyerProductCount = p.BuyerProductPackagings.Count,
                SupplierProductCount = p.SupplierProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (packageType is null)
        {
            return NotFound();
        }

        return View(packageType);
    }

    public IActionResult Create()
    {
        return View(new PackageTypeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PackageTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var packageType = new Data.Models.PackageType
        {
            Type = model.Type,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.PackageTypes.Add(packageType);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var packageType = await Context.PackageTypes.FindAsync(id);
        if (packageType is null)
        {
            return NotFound();
        }

        var model = new PackageTypeFormViewModel
        {
            Id = packageType.Id,
            Type = packageType.Type ?? string.Empty,
            IsActive = packageType.IsActive ?? false,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PackageTypeFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var packageType = await Context.PackageTypes.FindAsync(id);
        if (packageType is null)
        {
            return NotFound();
        }

        packageType.Type = model.Type;
        packageType.IsActive = model.IsActive;
        packageType.UpdatedOn = DateTime.UtcNow;
        packageType.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var packageType = await Context.PackageTypes.FindAsync(id);
        if (packageType is null)
        {
            return NotFound();
        }

        packageType.IsActive = false;
        packageType.UpdatedOn = DateTime.UtcNow;
        packageType.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
