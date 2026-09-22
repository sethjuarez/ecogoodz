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

        model.RateHistory = await Context.SupplierProductHistories
            .AsNoTracking()
            .Where(history => history.SupplierProductId == id)
            .OrderByDescending(history => history.CreatedDate)
            .ThenByDescending(history => history.Id)
            .Select(history => new RateChangeHistoryItemViewModel
            {
                OldPrice = history.OldPrice,
                NewPrice = history.NewPrice,
                OldEffectiveDate = history.OldEffectiveDate,
                NewEffectiveDate = history.NewEffectiveDate,
                CreatedDate = history.CreatedDate,
                UserName = history.User != null ? history.User.FirstName + " " + history.User.LastName : null,
                Action = history.Action,
            })
            .ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> AssignToBuyers(int id)
    {
        var model = await BuildAssignToBuyersModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> PropagateBuyerRates(int id, decimal? rate, DateTime? effectiveDate)
    {
        var model = await BuildRatePropagationModelAsync(id, rate, effectiveDate);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PropagateBuyerRates(SupplierProductRatePropagationViewModel model)
    {
        var selectedRows = model.BuyerProducts.Where(row => row.IsSelected).ToList();
        if (selectedRows.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one buyer product.");
        }

        for (var index = 0; index < model.BuyerProducts.Count; index++)
        {
            var row = model.BuyerProducts[index];
            if (!row.IsSelected)
            {
                continue;
            }

            if (!row.UpdatedRate.HasValue)
            {
                ModelState.AddModelError($"{nameof(model.BuyerProducts)}[{index}].{nameof(row.UpdatedRate)}", $"Enter an updated rate for {row.BuyerName}.");
            }

            if (!row.UpdatedEffectiveDate.HasValue)
            {
                ModelState.AddModelError($"{nameof(model.BuyerProducts)}[{index}].{nameof(row.UpdatedEffectiveDate)}", $"Enter an effective date for {row.BuyerName}.");
            }
        }

        var duplicatePostedProductIds = selectedRows
            .GroupBy(row => row.BuyerSupplierProductId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatePostedProductIds.Count != 0)
        {
            ModelState.AddModelError(string.Empty, "One or more buyer products was submitted more than once.");
        }

        var validProducts = await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product => product.SupplierProduct == model.SupplierProductId && product.BuyerSupplier != null && product.BuyerSupplier.IsActive == true)
            .Select(product => new
            {
                product.Id,
                CurrentRate = product.BuyerProductRates
                    .Where(productRate => productRate.IsActive && (productRate.EffectiveDate == null || productRate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(productRate => productRate.EffectiveDate)
                    .ThenByDescending(productRate => productRate.Id)
                    .Select(productRate => new { productRate.Price, productRate.EffectiveDate })
                    .FirstOrDefault(),
            })
            .ToListAsync();
        var validProductsById = validProducts.ToDictionary(product => product.Id);
        foreach (var row in selectedRows)
        {
            if (!validProductsById.ContainsKey(row.BuyerSupplierProductId))
            {
                ModelState.AddModelError(string.Empty, "One or more selected buyer products is no longer tied to this supplier product.");
            }
        }

        var duplicatePostedRates = selectedRows
            .Where(row => row.UpdatedEffectiveDate.HasValue)
            .GroupBy(row => new { row.BuyerSupplierProductId, EffectiveDate = row.UpdatedEffectiveDate!.Value.Date })
            .Where(group => group.Count() > 1)
            .ToList();
        foreach (var group in duplicatePostedRates)
        {
            var row = group.First();
            ModelState.AddModelError(string.Empty, $"The buyer rate for {row.BuyerName} on {group.Key.EffectiveDate:yyyy-MM-dd} was submitted more than once.");
        }

        foreach (var row in selectedRows.Where(row => row.UpdatedEffectiveDate.HasValue))
        {
            var duplicate = await Context.BuyerProductRates.AnyAsync(rate =>
                rate.BuyerSupplierProductId == row.BuyerSupplierProductId
                && rate.EffectiveDate.HasValue
                && rate.EffectiveDate.Value.Date == row.UpdatedEffectiveDate!.Value.Date);
            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, $"A buyer rate already exists on {row.UpdatedEffectiveDate:yyyy-MM-dd} for {row.BuyerName}.");
            }
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildRatePropagationModelAsync(model.SupplierProductId, model.SuggestedRate, model.SuggestedEffectiveDate);
            if (rebuilt is null)
            {
                return NotFound();
            }

            RestorePropagationRows(rebuilt.BuyerProducts, model.BuyerProducts);
            return View(rebuilt);
        }

        var userId = User.GetLegacyUserId();
        var now = DateTime.UtcNow;
        foreach (var row in selectedRows)
        {
            Context.BuyerProductRates.Add(new BuyerProductRate
            {
                BuyerSupplierProductId = row.BuyerSupplierProductId,
                Price = row.UpdatedRate,
                EffectiveDate = row.UpdatedEffectiveDate,
                CreatedDate = now,
                UserId = userId,
                IsActive = true,
            });
            Context.BuyerProductHistories.Add(new BuyerProductHistory
            {
                BuyerSupplierProductId = row.BuyerSupplierProductId,
                OldPrice = validProductsById[row.BuyerSupplierProductId].CurrentRate?.Price,
                NewPrice = row.UpdatedRate,
                OldEffectiveDate = validProductsById[row.BuyerSupplierProductId].CurrentRate?.EffectiveDate,
                NewEffectiveDate = row.UpdatedEffectiveDate,
                CreatedDate = now,
                UserId = userId,
                Action = "Add",
            });
        }

        await Context.SaveChangesAsync();
        TempData["Success"] = $"Updated {selectedRows.Count} buyer product rate{(selectedRows.Count == 1 ? string.Empty : "s")}.";

        return RedirectToAction(nameof(Details), new { id = model.SupplierProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToBuyers(SupplierProductAssignToBuyersViewModel model)
    {
        var supplierProduct = await Context.SupplierProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == model.ProductId);
        if (supplierProduct is null)
        {
            return NotFound();
        }

        ValidateAssignedBuyerRows(model.TiedBuyers, nameof(model.TiedBuyers));
        ValidateAssignedBuyerRows(model.ActiveBuyers, nameof(model.ActiveBuyers));

        if (!ModelState.IsValid)
        {
            var rebuiltModel = await BuildAssignToBuyersModelAsync(model.ProductId);
            if (rebuiltModel is null)
            {
                return NotFound();
            }

            RestoreAssignSelections(rebuiltModel.TiedBuyers, model.TiedBuyers);
            RestoreAssignSelections(rebuiltModel.ActiveBuyers, model.ActiveBuyers);
            return View(rebuiltModel);
        }

        var userId = User.GetLegacyUserId();
        var useTransaction = Context.Database.IsRelational();
        await using var transaction = useTransaction ? await Context.Database.BeginTransactionAsync() : null;
        var assignments = 0;

        assignments += await AssignToExistingBuyerMatchesAsync(supplierProduct, model.TiedBuyers, userId);
        assignments += await AssignToNewBuyerMatchesAsync(supplierProduct, model.ActiveBuyers, userId);

        if (assignments == 0)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync();
            }

            ModelState.AddModelError(string.Empty, "Select at least one buyer location.");
            var rebuiltModel = await BuildAssignToBuyersModelAsync(model.ProductId);
            if (rebuiltModel is null)
            {
                return NotFound();
            }

            RestoreAssignSelections(rebuiltModel.TiedBuyers, model.TiedBuyers);
            RestoreAssignSelections(rebuiltModel.ActiveBuyers, model.ActiveBuyers);
            return View(rebuiltModel);
        }

        await Context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        if (TempData is not null)
        {
            TempData["Success"] = "Product assigned to buyers.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ProductId });
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

        var userId = User.GetLegacyUserId();
        Context.SupplierProductRates.Add(new SupplierProductRate
        {
            SupplierProductId = model.SupplierProductId,
            Price = model.Price,
            EffectiveDate = model.EffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = userId,
            IsActive = true,
        });
        AddSupplierProductHistory(model.SupplierProductId, oldPrice: null, model.Price, oldEffectiveDate: null, model.EffectiveDate, userId, "Add");
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

        var oldPrice = rate.Price;
        var oldEffectiveDate = rate.EffectiveDate;
        var userId = User.GetLegacyUserId();

        rate.Price = model.Price;
        rate.EffectiveDate = model.EffectiveDate;
        rate.CreatedDate = DateTime.UtcNow;
        rate.UserId = userId;
        AddSupplierProductHistory(model.SupplierProductId, oldPrice, model.Price, oldEffectiveDate, model.EffectiveDate, userId, "Update");
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
        AddSupplierProductHistory(
            supplierProductId,
            rate.Price,
            null,
            rate.EffectiveDate,
            null,
            User.GetLegacyUserId(),
            "Delete");
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

    private void AddSupplierProductHistory(
        int supplierProductId,
        decimal? oldPrice,
        decimal? newPrice,
        DateTime? oldEffectiveDate,
        DateTime? newEffectiveDate,
        int? userId,
        string action)
    {
        Context.SupplierProductHistories.Add(new SupplierProductHistory
        {
        SupplierProductId = supplierProductId,
        OldPrice = oldPrice,
        NewPrice = newPrice,
        OldEffectiveDate = oldEffectiveDate,
        NewEffectiveDate = newEffectiveDate,
        CreatedDate = DateTime.UtcNow,
        UserId = userId,
        Action = action,
        });
    }

    private async Task<List<SelectListItem>> GetSelectedSupplierOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Suppliers.AsNoTracking().Where(s => s.Id == selectedId).Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedLocationOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Locations.AsNoTracking().Where(l => l.Id == selectedId).Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1, Selected = true }).ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedProductOptionsAsync(int? selectedId) =>
        selectedId is null ? [] : await Context.Products.AsNoTracking().Where(p => p.Id == selectedId).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name, Selected = true }).ToListAsync();

    private async Task<SupplierProductAssignToBuyersViewModel?> BuildAssignToBuyersModelAsync(int productId)
    {
        var supplierProduct = await Context.SupplierProducts
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new
            {
                product.Id,
                product.Supplier,
                product.Location,
                SupplierName = product.SupplierNavigation != null ? product.SupplierNavigation.Name : null,
                LocationName = product.LocationNavigation != null ? product.LocationNavigation.Location1 : null,
                ProductName = product.ProductNavigation != null ? product.ProductNavigation.Name : null,
                CurrentRate = product.SupplierProductRates
                    .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveDate)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => new { rate.Price, rate.EffectiveDate })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync();

        if (supplierProduct is null)
        {
            return null;
        }

        var tiedLocationIds = await Context.BuyerSuppliers
            .AsNoTracking()
            .Where(match =>
                match.Supplier == supplierProduct.Supplier
                && match.SupplierLocation == supplierProduct.Location
                && match.IsActive == true
                && match.BuyerLocation.HasValue)
            .Select(match => match.BuyerLocation!.Value)
            .Distinct()
            .ToListAsync();

        var tiedLocationIdSet = tiedLocationIds.ToHashSet();

        var tiedMatches = await Context.BuyerSuppliers
            .AsNoTracking()
            .Where(match =>
                match.Supplier == supplierProduct.Supplier
                && match.SupplierLocation == supplierProduct.Location
                && match.IsActive == true
                && !match.BuyerSupplierProducts.Any(product => product.SupplierProduct == productId))
            .Select(match => new
            {
                Buyer = match.Buyer,
                BuyerName = match.BuyerNavigation != null ? match.BuyerNavigation.Name : null,
                LocationId = match.BuyerLocation,
                LocationName = match.BuyerLocationNavigation != null ? match.BuyerLocationNavigation.Location1 : null,
            })
            .Select(match => new BuyerLocationRow(match.Buyer, match.BuyerName, match.LocationId, match.LocationName))
            .ToListAsync();

        var activeBuyerLocations = await (
                from buyer in Context.Buyers.AsNoTracking()
                join location in Context.Locations.AsNoTracking() on buyer.Id equals location.ClientId
                where buyer.IsActive == true
                    && location.IsActive
                    && location.IsBuyer == true
                    && (location.BuyerStatus == 2 || location.BuyerStatus == 3)
                    && !tiedLocationIdSet.Contains(location.Id)
                select new
                {
                    Buyer = (int?)buyer.Id,
                    BuyerName = buyer.Name,
                    LocationId = (int?)location.Id,
                    LocationName = location.Location1,
                })
            .Select(row => new BuyerLocationRow(row.Buyer, row.BuyerName, row.LocationId, row.LocationName))
            .ToListAsync();

        return new SupplierProductAssignToBuyersViewModel
        {
            ProductId = supplierProduct.Id,
            SupplierName = supplierProduct.SupplierName ?? string.Empty,
            SupplierLocationName = supplierProduct.LocationName ?? string.Empty,
            ProductName = supplierProduct.ProductName ?? string.Empty,
            ProductRate = supplierProduct.CurrentRate?.Price,
            ProductEffectiveDate = supplierProduct.CurrentRate?.EffectiveDate,
            TiedBuyers = BuildAssignBuyerRows(tiedMatches, supplierProduct.CurrentRate?.Price, supplierProduct.CurrentRate?.EffectiveDate),
            ActiveBuyers = BuildAssignBuyerRows(activeBuyerLocations, supplierProduct.CurrentRate?.Price, supplierProduct.CurrentRate?.EffectiveDate),
        };
    }

    private static List<SupplierProductAssignBuyerRowViewModel> BuildAssignBuyerRows(
        IEnumerable<BuyerLocationRow> rows,
        decimal? defaultRate,
        DateTime? defaultEffectiveDate)
    {
        return rows
            .Where(row => row.Buyer.HasValue && row.LocationId.HasValue)
            .GroupBy(row => new { row.Buyer, row.BuyerName })
            .OrderBy(group => group.Key.BuyerName)
            .Select(group => new SupplierProductAssignBuyerRowViewModel
            {
                BuyerId = group.Key.Buyer,
                BuyerName = group.Key.BuyerName ?? "(unnamed buyer)",
                Rate = defaultRate,
                EffectiveDate = defaultEffectiveDate ?? DateTime.Today,
                Locations = group
                    .OrderBy(row => row.LocationName)
                    .Select(row => new SupplierProductAssignLocationViewModel
                    {
                        Id = row.LocationId!.Value,
                        Name = row.LocationName ?? "(unnamed location)",
                    })
                    .ToList(),
            })
            .ToList();
    }

    private void ValidateAssignedBuyerRows(IEnumerable<SupplierProductAssignBuyerRowViewModel> rows, string prefix)
    {
        var index = 0;
        foreach (var row in rows)
        {
            if (row.IsSelected)
            {
                if (row.SelectedLocationIds.Count == 0)
                {
                    ModelState.AddModelError($"{prefix}[{index}].SelectedLocationIds", "Choose at least one location.");
                }

                if (row.Rate is null)
                {
                    ModelState.AddModelError($"{prefix}[{index}].Rate", "Enter a buyer rate.");
                }

                if (row.EffectiveDate is null)
                {
                    ModelState.AddModelError($"{prefix}[{index}].EffectiveDate", "Enter an effective date.");
                }
            }

            index++;
        }
    }

    private async Task<SupplierProductRatePropagationViewModel?> BuildRatePropagationModelAsync(int supplierProductId, decimal? rate, DateTime? effectiveDate)
    {
        var supplierProduct = await Context.SupplierProducts
            .AsNoTracking()
            .Where(product => product.Id == supplierProductId)
            .Select(product => new
            {
                product.Id,
                SupplierName = product.SupplierNavigation != null ? product.SupplierNavigation.Name : null,
                ProductName = product.ProductNavigation != null ? product.ProductNavigation.Name : null,
                CurrentRate = product.SupplierProductRates
                    .Where(productRate => productRate.IsActive)
                    .OrderByDescending(productRate => productRate.EffectiveDate)
                    .ThenByDescending(productRate => productRate.Id)
                    .Select(productRate => new { productRate.Price, productRate.EffectiveDate })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync();
        if (supplierProduct is null)
        {
            return null;
        }

        rate ??= supplierProduct.CurrentRate?.Price;
        effectiveDate ??= supplierProduct.CurrentRate?.EffectiveDate ?? DateTime.Today;

        var rows = await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product => product.SupplierProduct == supplierProductId && product.BuyerSupplier != null && product.BuyerSupplier.IsActive == true)
            .Select(product => new SupplierProductRatePropagationRowViewModel
            {
                BuyerSupplierProductId = product.Id,
                BuyerName = product.BuyerSupplier != null && product.BuyerSupplier.BuyerNavigation != null
                    ? product.BuyerSupplier.BuyerNavigation.Name ?? string.Empty
                    : string.Empty,
                BuyerLocationName = product.BuyerSupplier != null && product.BuyerSupplier.BuyerLocationNavigation != null
                    ? product.BuyerSupplier.BuyerLocationNavigation.Location1
                    : null,
                CurrentRate = product.BuyerProductRates
                    .Where(productRate => productRate.IsActive && (productRate.EffectiveDate == null || productRate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(productRate => productRate.EffectiveDate)
                    .ThenByDescending(productRate => productRate.Id)
                    .Select(productRate => productRate.Price)
                    .FirstOrDefault(),
                CurrentEffectiveDate = product.BuyerProductRates
                    .Where(productRate => productRate.IsActive && (productRate.EffectiveDate == null || productRate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(productRate => productRate.EffectiveDate)
                    .ThenByDescending(productRate => productRate.Id)
                    .Select(productRate => productRate.EffectiveDate)
                    .FirstOrDefault(),
                UpdatedRate = rate,
                UpdatedEffectiveDate = effectiveDate,
            })
            .OrderBy(row => row.BuyerName)
            .ThenBy(row => row.BuyerLocationName)
            .ToListAsync();

        return new SupplierProductRatePropagationViewModel
        {
            SupplierProductId = supplierProduct.Id,
            SupplierName = supplierProduct.SupplierName ?? string.Empty,
            ProductName = supplierProduct.ProductName ?? string.Empty,
            SuggestedRate = rate,
            SuggestedEffectiveDate = effectiveDate,
            BuyerProducts = rows,
        };
    }

    private static void RestorePropagationRows(
        List<SupplierProductRatePropagationRowViewModel> rebuiltRows,
        List<SupplierProductRatePropagationRowViewModel> postedRows)
    {
        var postedById = postedRows
            .GroupBy(row => row.BuyerSupplierProductId)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var row in rebuiltRows)
        {
            if (!postedById.TryGetValue(row.BuyerSupplierProductId, out var posted))
            {
                continue;
            }

            row.IsSelected = posted.IsSelected;
            row.UpdatedRate = posted.UpdatedRate;
            row.UpdatedEffectiveDate = posted.UpdatedEffectiveDate;
        }
    }

    private static void RestoreAssignSelections(
        List<SupplierProductAssignBuyerRowViewModel> rebuiltRows,
        IReadOnlyList<SupplierProductAssignBuyerRowViewModel> postedRows)
    {
        foreach (var row in rebuiltRows)
        {
            var posted = postedRows.FirstOrDefault(candidate => candidate.BuyerId == row.BuyerId);
            if (posted is null)
            {
                continue;
            }

            row.IsSelected = posted.IsSelected;
            row.SelectedLocationIds = posted.SelectedLocationIds;
            row.Rate = posted.Rate;
            row.EffectiveDate = posted.EffectiveDate;
        }
    }

    private async Task<int> AssignToExistingBuyerMatchesAsync(
        SupplierProduct supplierProduct,
        IEnumerable<SupplierProductAssignBuyerRowViewModel> rows,
        int? userId)
    {
        var count = 0;
        foreach (var row in rows.Where(row => row.IsSelected && row.BuyerId.HasValue && row.Rate.HasValue))
        {
            var rate = row.Rate.GetValueOrDefault();
            foreach (var locationId in row.SelectedLocationIds)
            {
                var match = await Context.BuyerSuppliers
                    .FirstOrDefaultAsync(candidate =>
                        candidate.Buyer == row.BuyerId
                        && candidate.BuyerLocation == locationId
                        && candidate.Supplier == supplierProduct.Supplier
                        && candidate.SupplierLocation == supplierProduct.Location
                        && candidate.IsActive == true);
                if (match is null)
                {
                    continue;
                }

                await AddBuyerSupplierProductAsync(match.Id, supplierProduct.Id, row.EffectiveDate, rate, userId);
                count++;
            }
        }

        return count;
    }

    private async Task<int> AssignToNewBuyerMatchesAsync(
        SupplierProduct supplierProduct,
        IEnumerable<SupplierProductAssignBuyerRowViewModel> rows,
        int? userId)
    {
        var count = 0;
        foreach (var row in rows.Where(row => row.IsSelected && row.BuyerId.HasValue && row.Rate.HasValue))
        {
            var rate = row.Rate.GetValueOrDefault();
            foreach (var locationId in row.SelectedLocationIds)
            {
                var locationIsAssignable = await Context.Locations.AnyAsync(location =>
                    location.Id == locationId
                    && location.ClientId == row.BuyerId
                    && location.IsActive
                    && location.IsBuyer == true
                    && (location.BuyerStatus == 2 || location.BuyerStatus == 3));
                if (!locationIsAssignable)
                {
                    continue;
                }

                var existingMatch = await Context.BuyerSuppliers
                    .FirstOrDefaultAsync(candidate =>
                        candidate.Buyer == row.BuyerId
                        && candidate.BuyerLocation == locationId
                        && candidate.Supplier == supplierProduct.Supplier
                        && candidate.SupplierLocation == supplierProduct.Location
                        && candidate.IsActive == true);
                if (existingMatch is not null)
                {
                    await AddBuyerSupplierProductAsync(existingMatch.Id, supplierProduct.Id, row.EffectiveDate, rate, userId);
                    count++;
                    continue;
                }

                var match = new Data.Models.BuyerSupplier
                {
                    Status = 3,
                    Buyer = row.BuyerId,
                    Supplier = supplierProduct.Supplier,
                    BuyerLocation = locationId,
                    SupplierLocation = supplierProduct.Location,
                    IsActive = true,
                    CreateOn = DateTime.UtcNow,
                    CreatedBy = userId,
                };
                Context.BuyerSuppliers.Add(match);
                await Context.SaveChangesAsync();

                await AddBuyerSupplierProductAsync(match.Id, supplierProduct.Id, row.EffectiveDate, rate, userId);
                count++;
            }
        }

        return count;
    }

    private async Task AddBuyerSupplierProductAsync(int buyerSupplierId, int supplierProductId, DateTime? effectiveDate, decimal rate, int? userId)
    {
        var existingProduct = await Context.BuyerSupplierProducts
            .Include(product => product.BuyerProductRates)
            .FirstOrDefaultAsync(product =>
                product.BuyerSupplierId == buyerSupplierId
                && product.SupplierProduct == supplierProductId);

        if (existingProduct is null)
        {
            existingProduct = new BuyerSupplierProduct
            {
                BuyerSupplierId = buyerSupplierId,
                SupplierProduct = supplierProductId,
                CreateOn = DateTime.UtcNow,
                CreatedBy = userId,
            };
            Context.BuyerSupplierProducts.Add(existingProduct);
            await Context.SaveChangesAsync();
        }

        var duplicateRate = effectiveDate.HasValue
            ? existingProduct.BuyerProductRates.Any(existing =>
                existing.EffectiveDate.HasValue
                && existing.EffectiveDate.Value.Date == effectiveDate.Value.Date)
            : existingProduct.BuyerProductRates.Any(existing => !existing.EffectiveDate.HasValue);
        if (!duplicateRate)
        {
            existingProduct.BuyerProductRates.Add(new BuyerProductRate
            {
                Price = rate,
                EffectiveDate = effectiveDate,
                CreatedDate = DateTime.UtcNow,
                UserId = userId,
                IsActive = true,
            });
        }
    }

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

    private sealed record BuyerLocationRow(int? Buyer, string? BuyerName, int? LocationId, string? LocationName);
}
