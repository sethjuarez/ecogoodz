using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.BuyerProduct;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class BuyerProductController : PagedListController<BuyerProductController.BuyerProductRow, BuyerProductListItemViewModel>
{
    public BuyerProductController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<BuyerProductRow> GetBaseQuery() =>
        from buyerProduct in Context.BuyerProducts
        join buyer in Context.Buyers on buyerProduct.Buyer equals buyer.Id into buyerJoin
        from buyer in buyerJoin.DefaultIfEmpty()
        join location in Context.Locations on buyerProduct.Location equals location.Id into locationJoin
        from location in locationJoin.DefaultIfEmpty()
        join product in Context.Products on buyerProduct.Product equals product.Id into productJoin
        from product in productJoin.DefaultIfEmpty()
        select new BuyerProductRow
        {
            BuyerProduct = buyerProduct,
            BuyerName = buyer.Name,
            BuyerNameSort = buyer != null ? EF.Property<string>(buyer, "NameSort") : null,
            LocationName = location.Location1,
            LocationNameSort = location != null ? EF.Property<string>(location, "LocationSort") : null,
            ProductName = product.Name,
            ProductNameSort = product != null ? EF.Property<string>(product, "NameSort") : null,
        };

    protected override IQueryable<BuyerProductRow> ApplySearch(IQueryable<BuyerProductRow> query, string searchTerm) =>
        query.Where(r =>
            (r.BuyerName != null && r.BuyerName.Contains(searchTerm))
            || (r.LocationName != null && r.LocationName.Contains(searchTerm))
            || (r.ProductName != null && r.ProductName.Contains(searchTerm))
            || (r.BuyerProduct.OtherProduct != null && r.BuyerProduct.OtherProduct.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<BuyerProductRow, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<BuyerProductRow, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["buyer"] = r => r.BuyerNameSort,
            ["location"] = r => r.LocationNameSort,
            ["product"] = r => r.ProductNameSort ?? r.BuyerProduct.OtherProduct,
            ["active"] = r => r.BuyerProduct.IsActive,
        };

    protected override string DefaultSortColumn => "buyer";

    protected override Expression<Func<BuyerProductRow, BuyerProductListItemViewModel>> ProjectionExpression =>
        r => new BuyerProductListItemViewModel
        {
            Id = r.BuyerProduct.Id,
            BuyerName = r.BuyerName ?? string.Empty,
            LocationName = r.LocationName ?? string.Empty,
            ProductName = r.ProductName ?? r.BuyerProduct.OtherProduct ?? "(other product)",
            PackagingName = r.BuyerProduct.BuyerProductPackagings
                .Select(p => p.PackagingNavigation != null ? p.PackagingNavigation.Type : p.OtherPackaging)
                .FirstOrDefault(),
            IsActive = r.BuyerProduct.IsActive,
        };

    public async Task<IActionResult> Details(int id)
    {
        var model = await GetBaseQuery()
            .Where(r => r.BuyerProduct.Id == id)
            .Select(r => new BuyerProductDetailsViewModel
            {
                Id = r.BuyerProduct.Id,
                BuyerName = r.BuyerName ?? string.Empty,
                LocationName = r.LocationName ?? string.Empty,
                ProductName = r.ProductName ?? r.BuyerProduct.OtherProduct ?? "(other product)",
                IsActive = r.BuyerProduct.IsActive,
                CreateOn = r.BuyerProduct.CreateOn,
                UpdatedOn = r.BuyerProduct.UpdatedOn,
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        model.PackagingNames = await Context.BuyerProductPackagings
            .AsNoTracking()
            .Where(p => p.BuyerProduct == id)
            .Select(p => p.PackagingNavigation != null ? p.PackagingNavigation.Type : p.OtherPackaging)
            .Where(p => p != null)
            .Select(p => p!)
            .ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = new BuyerProductFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuyerProductFormViewModel model)
    {
        ValidateProductSelection(model);
        ValidatePackagingSelection(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var buyerProduct = new BuyerProduct
        {
            Buyer = model.Buyer,
            Location = model.Location,
            Product = model.Product,
            OtherProduct = string.IsNullOrWhiteSpace(model.OtherProduct) ? null : model.OtherProduct.Trim(),
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.BuyerProducts.Add(buyerProduct);
        await Context.SaveChangesAsync();
        await SavePackagingAsync(buyerProduct.Id, model);

        return RedirectToAction(nameof(Details), new { id = buyerProduct.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var buyerProduct = await Context.BuyerProducts
            .Include(p => p.BuyerProductPackagings)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (buyerProduct is null)
        {
            return NotFound();
        }

        var packaging = buyerProduct.BuyerProductPackagings.FirstOrDefault();
        var model = new BuyerProductFormViewModel
        {
            Id = buyerProduct.Id,
            Buyer = buyerProduct.Buyer,
            Location = buyerProduct.Location,
            Product = buyerProduct.Product,
            OtherProduct = buyerProduct.OtherProduct,
            Packaging = packaging?.Packaging,
            OtherPackaging = packaging?.OtherPackaging,
            IsActive = buyerProduct.IsActive,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuyerProductFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        ValidateProductSelection(model);
        ValidatePackagingSelection(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var buyerProduct = await Context.BuyerProducts.FindAsync(id);
        if (buyerProduct is null)
        {
            return NotFound();
        }

        buyerProduct.Buyer = model.Buyer;
        buyerProduct.Location = model.Location;
        buyerProduct.Product = model.Product;
        buyerProduct.OtherProduct = string.IsNullOrWhiteSpace(model.OtherProduct) ? null : model.OtherProduct.Trim();
        buyerProduct.IsActive = model.IsActive;
        buyerProduct.UpdatedOn = DateTime.UtcNow;
        buyerProduct.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();
        await SavePackagingAsync(id, model);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var buyerProduct = await Context.BuyerProducts.FindAsync(id);
        if (buyerProduct is null)
        {
            return NotFound();
        }

        buyerProduct.IsActive = false;
        buyerProduct.UpdatedOn = DateTime.UtcNow;
        buyerProduct.UpdatedBy = User.GetLegacyUserId();
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> SearchBuyers(string? q)
    {
        var query = Context.Buyers.AsNoTracking().Where(b => b.IsActive == true);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(b => b.Name != null && b.Name.Contains(q));
        }

        return Json(await query.OrderBy(b => b.Name).Take(50).Select(b => new SelectOption(b.Id.ToString(), b.Name ?? "(unnamed buyer)")).ToListAsync());
    }

    public async Task<IActionResult> SearchLocations(string? q)
    {
        var query = Context.Locations.AsNoTracking().Where(l => l.IsActive && l.IsBuyer == true);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(l => (l.Location1 != null && l.Location1.Contains(q)) || (l.City != null && l.City.Contains(q)));
        }

        return Json(await query.OrderBy(l => l.Location1).Take(50).Select(l => new SelectOption(l.Id.ToString(), l.Location1 ?? "(unnamed location)")).ToListAsync());
    }

    public async Task<IActionResult> SearchProducts(string? q)
    {
        var query = Context.Products.AsNoTracking().Where(p => p.IsActive == true);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Name != null && p.Name.Contains(q));
        }

        return Json(await query.OrderBy(p => p.Name).Take(50).Select(p => new SelectOption(p.Id.ToString(), p.Name ?? "(unnamed product)")).ToListAsync());
    }

    private async Task PopulateOptionsAsync(BuyerProductFormViewModel model)
    {
        model.BuyerOptions = await GetSelectedBuyerOptionsAsync(model.Buyer);
        model.LocationOptions = await GetSelectedLocationOptionsAsync(model.Location);
        model.ProductOptions = await GetSelectedProductOptionsAsync(model.Product);
        model.PackagingOptions = await Context.PackageTypes
            .AsNoTracking()
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Type)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Type })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetSelectedBuyerOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Buyers.AsNoTracking().Where(b => b.Id == selectedId).Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedLocationOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Locations.AsNoTracking().Where(l => l.Id == selectedId).Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedProductOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Products.AsNoTracking().Where(p => p.Id == selectedId).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name, Selected = true }).ToListAsync();

    private void ValidateProductSelection(BuyerProductFormViewModel model)
    {
        if (model.Product is null && string.IsNullOrWhiteSpace(model.OtherProduct))
        {
            ModelState.AddModelError(nameof(model.Product), "Choose a product or enter an other product name.");
        }
    }

    private void ValidatePackagingSelection(BuyerProductFormViewModel model)
    {
        if (model.Packaging is null && string.IsNullOrWhiteSpace(model.OtherPackaging))
        {
            return;
        }

        if (model.Packaging is not null && !string.IsNullOrWhiteSpace(model.OtherPackaging))
        {
            ModelState.AddModelError(nameof(model.OtherPackaging), "Use either a package type or other packaging, not both.");
        }
    }

    private async Task SavePackagingAsync(int buyerProductId, BuyerProductFormViewModel model)
    {
        var existing = Context.BuyerProductPackagings.Where(p => p.BuyerProduct == buyerProductId);
        Context.BuyerProductPackagings.RemoveRange(existing);

        if (model.Packaging is not null || !string.IsNullOrWhiteSpace(model.OtherPackaging))
        {
            Context.BuyerProductPackagings.Add(new BuyerProductPackaging
            {
                BuyerProduct = buyerProductId,
                Packaging = model.Packaging,
                OtherPackaging = string.IsNullOrWhiteSpace(model.OtherPackaging) ? null : model.OtherPackaging.Trim(),
            });
        }

        await Context.SaveChangesAsync();
    }

    public sealed class BuyerProductRow
    {
        public required BuyerProduct BuyerProduct { get; init; }
        public string? BuyerName { get; init; }
        public string? BuyerNameSort { get; init; }
        public string? LocationName { get; init; }
        public string? LocationNameSort { get; init; }
        public string? ProductName { get; init; }
        public string? ProductNameSort { get; init; }
    }

    private sealed record SelectOption(string Value, string Text);
}
