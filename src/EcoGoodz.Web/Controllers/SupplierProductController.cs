using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Shared;
using EcoGoodz.Web.Models.SupplierProduct;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class SupplierProductController : PagedListController<SupplierProductController.SupplierProductRow, SupplierProductListItemViewModel>
{
    public SupplierProductController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<SupplierProductRow> GetBaseQuery() =>
        from supplierProduct in Context.SupplierProducts
        join supplier in Context.Suppliers on supplierProduct.Supplier equals supplier.Id into supplierJoin
        from supplier in supplierJoin.DefaultIfEmpty()
        join location in Context.Locations on supplierProduct.Location equals location.Id into locationJoin
        from location in locationJoin.DefaultIfEmpty()
        join product in Context.Products on supplierProduct.Product equals product.Id into productJoin
        from product in productJoin.DefaultIfEmpty()
        join packaging in Context.PackageTypes on supplierProduct.Packaging equals packaging.Id into packagingJoin
        from packaging in packagingJoin.DefaultIfEmpty()
        select new SupplierProductRow
        {
            SupplierProduct = supplierProduct,
            SupplierName = supplier.Name,
            LocationName = location.Location1,
            ProductName = product.Name,
            PackagingName = packaging.Type,
        };

    protected override IQueryable<SupplierProductRow> ApplySearch(IQueryable<SupplierProductRow> query, string searchTerm) =>
        query.Where(r =>
            (r.SupplierName != null && r.SupplierName.Contains(searchTerm))
            || (r.LocationName != null && r.LocationName.Contains(searchTerm))
            || (r.ProductName != null && r.ProductName.Contains(searchTerm))
            || (r.PackagingName != null && r.PackagingName.Contains(searchTerm))
            || (r.SupplierProduct.OtherPackaging != null && r.SupplierProduct.OtherPackaging.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<SupplierProductRow, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<SupplierProductRow, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["supplier"] = r => r.SupplierName,
            ["location"] = r => r.LocationName,
            ["product"] = r => r.ProductName,
            ["packaging"] = r => r.PackagingName ?? r.SupplierProduct.OtherPackaging,
            ["active"] = r => r.SupplierProduct.IsActive,
        };

    protected override string DefaultSortColumn => "supplier";

    protected override Expression<Func<SupplierProductRow, SupplierProductListItemViewModel>> ProjectionExpression =>
        r => new SupplierProductListItemViewModel
        {
            Id = r.SupplierProduct.Id,
            SupplierName = r.SupplierName ?? string.Empty,
            LocationName = r.LocationName ?? string.Empty,
            ProductName = r.ProductName ?? string.Empty,
            PackagingName = r.PackagingName ?? r.SupplierProduct.OtherPackaging,
            CurrentPrice = r.SupplierProduct.SupplierProductRates
                .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                .OrderByDescending(rate => rate.EffectiveDate)
                .ThenByDescending(rate => rate.Id)
                .Select(rate => rate.Price)
                .FirstOrDefault(),
            CurrentEffectiveDate = r.SupplierProduct.SupplierProductRates
                .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                .OrderByDescending(rate => rate.EffectiveDate)
                .ThenByDescending(rate => rate.Id)
                .Select(rate => rate.EffectiveDate)
                .FirstOrDefault(),
            IsActive = r.SupplierProduct.IsActive,
        };

    public override async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(sort) || desc)
        {
            return await base.Index(search, sort, desc, page, pageSize);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;

        var totalCount = await Context.SupplierProducts.AsNoTracking().CountAsync();
        var pageSupplierProducts = Context.SupplierProducts
            .AsNoTracking()
            .OrderBy(supplierProduct => supplierProduct.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
                from supplierProduct in pageSupplierProducts
                join supplier in Context.Suppliers.AsNoTracking() on supplierProduct.Supplier equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                join location in Context.Locations.AsNoTracking() on supplierProduct.Location equals location.Id into locationJoin
                from location in locationJoin.DefaultIfEmpty()
                join product in Context.Products.AsNoTracking() on supplierProduct.Product equals product.Id into productJoin
                from product in productJoin.DefaultIfEmpty()
                join packaging in Context.PackageTypes.AsNoTracking() on supplierProduct.Packaging equals packaging.Id into packagingJoin
                from packaging in packagingJoin.DefaultIfEmpty()
                orderby supplierProduct.Id
                select new SupplierProductListItemViewModel
                {
                    Id = supplierProduct.Id,
                    SupplierName = supplier.Name ?? string.Empty,
                    LocationName = location.Location1 ?? string.Empty,
                    ProductName = product.Name ?? string.Empty,
                    PackagingName = packaging.Type ?? supplierProduct.OtherPackaging,
                    CurrentPrice = supplierProduct.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.Price)
                        .FirstOrDefault(),
                    CurrentEffectiveDate = supplierProduct.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.EffectiveDate)
                        .FirstOrDefault(),
                    IsActive = supplierProduct.IsActive,
                })
            .ToListAsync();

        return View("Index", new PagedResult<SupplierProductListItemViewModel>
        {
            Items = items,
            Page = new PageInfo
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search,
            },
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await GetBaseQuery()
            .Where(r => r.SupplierProduct.Id == id)
            .Select(r => new SupplierProductDetailsViewModel
            {
                Id = r.SupplierProduct.Id,
                SupplierName = r.SupplierName ?? string.Empty,
                LocationName = r.LocationName ?? string.Empty,
                ProductName = r.ProductName ?? string.Empty,
                PackagingName = r.PackagingName ?? r.SupplierProduct.OtherPackaging,
                PackagingPrice = r.SupplierProduct.PackagingPrice,
                Volume = r.SupplierProduct.Volume,
                FrequencyLabel = r.SupplierProduct.FrequencyPackageTypeNavigation != null
                    ? r.SupplierProduct.Frequency + " " + r.SupplierProduct.FrequencyPackageTypeNavigation.Type
                    : r.SupplierProduct.Frequency != null ? r.SupplierProduct.Frequency.ToString() : null,
                IsActive = r.SupplierProduct.IsActive,
                CreateOn = r.SupplierProduct.CreateOn,
                UpdatedOn = r.SupplierProduct.UpdatedOn,
                NewRate = new SupplierProductRateFormViewModel { SupplierProductId = id, EffectiveDate = DateTime.Today },
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        model.Rates = await Context.SupplierProductRates
            .AsNoTracking()
            .Where(rate => rate.SupplierProductId == id)
            .OrderByDescending(rate => rate.EffectiveDate)
            .Select(rate => new SupplierProductRateListItemViewModel
            {
                Id = rate.Id,
                Price = rate.Price,
                EffectiveDate = rate.EffectiveDate,
                CreatedDate = rate.CreatedDate,
                UserName = rate.User != null ? rate.User.FirstName + " " + rate.User.LastName : null,
                IsActive = rate.IsActive,
            })
            .ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = new SupplierProductFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierProductFormViewModel model)
    {
        ValidateSupplierProduct(model, requireInitialRate: true);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var supplierProduct = new SupplierProduct
        {
            Supplier = model.Supplier,
            Location = model.Location,
            Product = model.Product,
            Packaging = model.Packaging,
            OtherPackaging = string.IsNullOrWhiteSpace(model.OtherPackaging) ? null : model.OtherPackaging.Trim(),
            PackagingPrice = model.PackagingPrice,
            Volume = model.Volume,
            Frequency = model.Frequency,
            FrequencyPackageType = model.FrequencyPackageType,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.SupplierProducts.Add(supplierProduct);
        await Context.SaveChangesAsync();

        Context.SupplierProductRates.Add(new SupplierProductRate
        {
            SupplierProductId = supplierProduct.Id,
            Price = model.InitialPrice,
            EffectiveDate = model.InitialEffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = User.GetLegacyUserId(),
            IsActive = true,
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = supplierProduct.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplierProduct = await Context.SupplierProducts.FindAsync(id);
        if (supplierProduct is null)
        {
            return NotFound();
        }

        var model = new SupplierProductFormViewModel
        {
            Id = supplierProduct.Id,
            Supplier = supplierProduct.Supplier,
            Location = supplierProduct.Location,
            Product = supplierProduct.Product,
            Packaging = supplierProduct.Packaging,
            OtherPackaging = supplierProduct.OtherPackaging,
            PackagingPrice = supplierProduct.PackagingPrice,
            Volume = supplierProduct.Volume,
            Frequency = supplierProduct.Frequency,
            FrequencyPackageType = supplierProduct.FrequencyPackageType,
            IsActive = supplierProduct.IsActive,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierProductFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        ValidateSupplierProduct(model, requireInitialRate: false);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var supplierProduct = await Context.SupplierProducts.FindAsync(id);
        if (supplierProduct is null)
        {
            return NotFound();
        }

        supplierProduct.Supplier = model.Supplier;
        supplierProduct.Location = model.Location;
        supplierProduct.Product = model.Product;
        supplierProduct.Packaging = model.Packaging;
        supplierProduct.OtherPackaging = string.IsNullOrWhiteSpace(model.OtherPackaging) ? null : model.OtherPackaging.Trim();
        supplierProduct.PackagingPrice = model.PackagingPrice;
        supplierProduct.Volume = model.Volume;
        supplierProduct.Frequency = model.Frequency;
        supplierProduct.FrequencyPackageType = model.FrequencyPackageType;
        supplierProduct.IsActive = model.IsActive;
        supplierProduct.UpdatedOn = DateTime.UtcNow;
        supplierProduct.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRate(SupplierProductRateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid supplier rate before saving.";
            return RedirectToAction(nameof(Details), new { id = model.SupplierProductId });
        }

        var supplierProductExists = await Context.SupplierProducts.AnyAsync(product => product.Id == model.SupplierProductId);
        if (!supplierProductExists)
        {
            return NotFound();
        }

        var duplicate = await Context.SupplierProductRates.AnyAsync(rate =>
            rate.SupplierProductId == model.SupplierProductId
            && rate.EffectiveDate.HasValue
            && model.EffectiveDate.HasValue
            && rate.EffectiveDate.Value.Date == model.EffectiveDate.Value.Date);

        if (duplicate)
        {
            TempData["Error"] = "A rate already exists for that effective date.";
            return RedirectToAction(nameof(Details), new { id = model.SupplierProductId });
        }

        Context.SupplierProductRates.Add(new SupplierProductRate
        {
            SupplierProductId = model.SupplierProductId,
            Price = model.Price,
            EffectiveDate = model.EffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = User.GetLegacyUserId(),
            IsActive = true,
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.SupplierProductId });
    }

    public async Task<IActionResult> EditRate(int id)
    {
        var rate = await Context.SupplierProductRates
            .AsNoTracking()
            .Where(rate => rate.Id == id)
            .Select(rate => new SupplierProductRateFormViewModel
            {
                Id = rate.Id,
                SupplierProductId = rate.SupplierProductId ?? 0,
                Price = rate.Price,
                EffectiveDate = rate.EffectiveDate,
            })
            .FirstOrDefaultAsync();

        if (rate is null)
        {
            return NotFound();
        }

        return View(rate);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRate(int id, SupplierProductRateFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rate = await Context.SupplierProductRates.FindAsync(id);
        if (rate is null || rate.SupplierProductId != model.SupplierProductId)
        {
            return NotFound();
        }

        var duplicate = await Context.SupplierProductRates.AnyAsync(existing =>
            existing.Id != id
            && existing.SupplierProductId == model.SupplierProductId
            && existing.EffectiveDate.HasValue
            && model.EffectiveDate.HasValue
            && existing.EffectiveDate.Value.Date == model.EffectiveDate.Value.Date);

        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.EffectiveDate), "A rate already exists for that effective date.");
            return View(model);
        }

        rate.Price = model.Price;
        rate.EffectiveDate = model.EffectiveDate;
        rate.CreatedDate = DateTime.UtcNow;
        rate.UserId = User.GetLegacyUserId();
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.SupplierProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var supplierProduct = await Context.SupplierProducts.FindAsync(id);
        if (supplierProduct is null)
        {
            return NotFound();
        }

        supplierProduct.IsActive = false;
        supplierProduct.UpdatedOn = DateTime.UtcNow;
        supplierProduct.UpdatedBy = User.GetLegacyUserId();
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateRate(int id, int supplierProductId)
    {
        var rate = await Context.SupplierProductRates.FindAsync(id);
        if (rate is null || rate.SupplierProductId != supplierProductId)
        {
            return NotFound();
        }

        rate.IsActive = false;
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = supplierProductId });
    }

    public async Task<IActionResult> SearchSuppliers(string? q)
    {
        var query = Context.Suppliers.AsNoTracking().Where(s => s.IsActive == true);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(s => s.Name != null && s.Name.Contains(q));
        }

        return Json(await query.OrderBy(s => s.Name).Take(50).Select(s => new SelectOption(s.Id.ToString(), s.Name ?? "(unnamed supplier)")).ToListAsync());
    }

    public async Task<IActionResult> SearchLocations(string? q)
    {
        var query = Context.Locations.AsNoTracking().Where(l => l.IsActive && l.IsBuyer == false);
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

    private async Task PopulateOptionsAsync(SupplierProductFormViewModel model)
    {
        model.SupplierOptions = await GetSelectedSupplierOptionsAsync(model.Supplier);
        model.LocationOptions = await GetSelectedLocationOptionsAsync(model.Location);
        model.ProductOptions = await GetSelectedProductOptionsAsync(model.Product);
        model.PackagingOptions = await Context.PackageTypes.AsNoTracking().Where(p => p.IsActive == true).OrderBy(p => p.Type).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Type }).ToListAsync();
        model.FrequencyPackageTypeOptions = await Context.FrequencyPackageTypes.AsNoTracking().OrderBy(p => p.Type).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Type }).ToListAsync();
    }

    private async Task<List<SelectListItem>> GetSelectedSupplierOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Suppliers.AsNoTracking().Where(s => s.Id == selectedId).Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedLocationOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Locations.AsNoTracking().Where(l => l.Id == selectedId).Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedProductOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Products.AsNoTracking().Where(p => p.Id == selectedId).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name, Selected = true }).ToListAsync();

    private void ValidateSupplierProduct(SupplierProductFormViewModel model, bool requireInitialRate)
    {
        if (model.Supplier is null)
        {
            ModelState.AddModelError(nameof(model.Supplier), "Choose a supplier.");
        }

        if (model.Location is null)
        {
            ModelState.AddModelError(nameof(model.Location), "Choose a location.");
        }

        if (model.Product is null)
        {
            ModelState.AddModelError(nameof(model.Product), "Choose a product.");
        }

        if (model.Packaging is not null && !string.IsNullOrWhiteSpace(model.OtherPackaging))
        {
            ModelState.AddModelError(nameof(model.OtherPackaging), "Use either a package type or other packaging, not both.");
        }

        if (requireInitialRate)
        {
            if (model.InitialPrice is null)
            {
                ModelState.AddModelError(nameof(model.InitialPrice), "Enter the initial product price.");
            }

            if (model.InitialEffectiveDate is null)
            {
                ModelState.AddModelError(nameof(model.InitialEffectiveDate), "Enter the initial effective date.");
            }
        }
    }

    public sealed class SupplierProductRow
    {
        public required SupplierProduct SupplierProduct { get; init; }
        public string? SupplierName { get; init; }
        public string? LocationName { get; init; }
        public string? ProductName { get; init; }
        public string? PackagingName { get; init; }
    }

    private sealed record SelectOption(string Value, string Text);
}
