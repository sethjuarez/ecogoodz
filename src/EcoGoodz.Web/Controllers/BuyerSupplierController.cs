using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.BuyerSupplier;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class BuyerSupplierController : PagedListController<Data.Models.BuyerSupplier, BuyerSupplierListItemViewModel>
{
    public BuyerSupplierController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.BuyerSupplier> GetBaseQuery() =>
        Context.BuyerSuppliers
            .Include(m => m.BuyerNavigation)
            .Include(m => m.SupplierNavigation)
            .Include(m => m.BuyerLocationNavigation)
            .Include(m => m.SupplierLocationNavigation)
            .Include(m => m.StatusNavigation);

    protected override IQueryable<Data.Models.BuyerSupplier> ApplySearch(IQueryable<Data.Models.BuyerSupplier> query, string searchTerm) =>
        query.Where(m =>
            (m.BuyerNavigation != null && m.BuyerNavigation.Name != null && m.BuyerNavigation.Name.Contains(searchTerm))
            || (m.SupplierNavigation != null && m.SupplierNavigation.Name != null && m.SupplierNavigation.Name.Contains(searchTerm))
            || (m.BuyerLocationNavigation != null && m.BuyerLocationNavigation.Location1 != null && m.BuyerLocationNavigation.Location1.Contains(searchTerm))
            || (m.SupplierLocationNavigation != null && m.SupplierLocationNavigation.Location1 != null && m.SupplierLocationNavigation.Location1.Contains(searchTerm))
            || (m.StatusNavigation != null && m.StatusNavigation.Status != null && m.StatusNavigation.Status.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.BuyerSupplier, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.BuyerSupplier, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["buyer"] = m => m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
            ["supplier"] = m => m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
            ["buyerLocation"] = m => m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
            ["supplierLocation"] = m => m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
            ["status"] = m => m.StatusNavigation != null ? m.StatusNavigation.Status : null,
            ["active"] = m => m.IsActive,
        };

    protected override string DefaultSortColumn => "buyer";

    protected override Expression<Func<Data.Models.BuyerSupplier, BuyerSupplierListItemViewModel>> ProjectionExpression =>
        m => new BuyerSupplierListItemViewModel
        {
            Id = m.Id,
            BuyerId = m.Buyer,
            SupplierId = m.Supplier,
            BuyerLocationId = m.BuyerLocation,
            SupplierLocationId = m.SupplierLocation,
            BuyerName = m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
            SupplierName = m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
            BuyerLocationName = m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
            SupplierLocationName = m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
            StatusName = m.StatusNavigation != null ? m.StatusNavigation.Status : null,
            IsActive = m.IsActive ?? false,
        };

    public override async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(sort) || desc)
        {
            return await base.Index(search, sort, desc, page, pageSize);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;

        var totalCount = await Context.BuyerSuppliers.AsNoTracking().CountAsync();
        var pageMatches = Context.BuyerSuppliers
            .AsNoTracking()
            .OrderBy(match => match.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
                from match in pageMatches
                join buyer in Context.Buyers.AsNoTracking() on match.Buyer equals buyer.Id into buyerJoin
                from buyer in buyerJoin.DefaultIfEmpty()
                join supplier in Context.Suppliers.AsNoTracking() on match.Supplier equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                join buyerLocation in Context.Locations.AsNoTracking() on match.BuyerLocation equals buyerLocation.Id into buyerLocationJoin
                from buyerLocation in buyerLocationJoin.DefaultIfEmpty()
                join supplierLocation in Context.Locations.AsNoTracking() on match.SupplierLocation equals supplierLocation.Id into supplierLocationJoin
                from supplierLocation in supplierLocationJoin.DefaultIfEmpty()
                join status in Context.SupplierBuyerStatuses.AsNoTracking() on match.Status equals status.Id into statusJoin
                from status in statusJoin.DefaultIfEmpty()
                orderby match.Id
                select new BuyerSupplierListItemViewModel
                {
                    Id = match.Id,
                    BuyerId = match.Buyer,
                    SupplierId = match.Supplier,
                    BuyerLocationId = match.BuyerLocation,
                    SupplierLocationId = match.SupplierLocation,
                    BuyerName = buyer.Name,
                    SupplierName = supplier.Name,
                    BuyerLocationName = buyerLocation.Location1,
                    SupplierLocationName = supplierLocation.Location1,
                    StatusName = status.Status,
                    IsActive = match.IsActive ?? false,
                })
            .ToListAsync();

        return View("Index", new PagedResult<BuyerSupplierListItemViewModel>
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
        var match = await Context.BuyerSuppliers
            .Where(m => m.Id == id)
            .Select(m => new BuyerSupplierDetailsViewModel
            {
                Id = m.Id,
                BuyerId = m.Buyer,
                SupplierId = m.Supplier,
                BuyerLocationId = m.BuyerLocation,
                SupplierLocationId = m.SupplierLocation,
                BuyerName = m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
                SupplierName = m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
                BuyerLocationName = m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
                SupplierLocationName = m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
                StatusName = m.StatusNavigation != null ? m.StatusNavigation.Status : null,
                IsActive = m.IsActive ?? false,
                CreateOn = m.CreateOn,
                UpdatedOn = m.UpdatedOn,
                ProductCount = m.BuyerSupplierProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (match is null)
        {
            return NotFound();
        }

        var matchKeys = await Context.BuyerSuppliers
            .Where(m => m.Id == id)
            .Select(m => new { m.Buyer, m.Supplier, m.BuyerLocation, m.SupplierLocation })
            .FirstAsync();

        match.LoadCount = await Context.Loads.CountAsync(l =>
            l.Buyer == matchKeys.Buyer
            && l.Supplier == matchKeys.Supplier
            && l.BuyerLocation == matchKeys.BuyerLocation
            && l.SupplierLocation == matchKeys.SupplierLocation);

        match.Products = await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product => product.BuyerSupplierId == id)
            .Select(product => new BuyerSupplierProductListItemViewModel
            {
                Id = product.Id,
                ProductName = product.SupplierProductNavigation != null && product.SupplierProductNavigation.ProductNavigation != null
                    ? product.SupplierProductNavigation.ProductNavigation.Name ?? string.Empty
                    : string.Empty,
                PackagingName = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.PackagingNavigation != null
                        ? product.SupplierProductNavigation.PackagingNavigation.Type
                        : product.SupplierProductNavigation.OtherPackaging
                    : null,
                BuyerPrice = product.BuyerProductRates
                    .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveDate)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => rate.Price)
                    .FirstOrDefault(),
                BuyerEffectiveDate = product.BuyerProductRates
                    .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveDate)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => rate.EffectiveDate)
                    .FirstOrDefault(),
                SupplierPrice = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.Price)
                        .FirstOrDefault()
                    : null,
                SupplierEffectiveDate = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.EffectiveDate)
                        .FirstOrDefault()
                    : null,
            })
            .OrderBy(product => product.ProductName)
            .ToListAsync();
        match.NewProduct = new BuyerSupplierProductFormViewModel
        {
            BuyerSupplierId = id,
            EffectiveDate = DateTime.Today,
        };

        return View(match);
    }

    public async Task<IActionResult> Create()
    {
        var model = new BuyerSupplierFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuyerSupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        await ValidateSelectionsAsync(model);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var match = new Data.Models.BuyerSupplier
        {
            Buyer = model.Buyer,
            Supplier = model.Supplier,
            BuyerLocation = model.BuyerLocation,
            SupplierLocation = model.SupplierLocation,
            Status = model.Status,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.BuyerSuppliers.Add(match);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        var model = new BuyerSupplierFormViewModel
        {
            Id = match.Id,
            Buyer = match.Buyer,
            Supplier = match.Supplier,
            BuyerLocation = match.BuyerLocation,
            SupplierLocation = match.SupplierLocation,
            Status = match.Status,
            IsActive = match.IsActive ?? false,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuyerSupplierFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        await ValidateSelectionsAsync(model);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        match.Buyer = model.Buyer;
        match.Supplier = model.Supplier;
        match.BuyerLocation = model.BuyerLocation;
        match.SupplierLocation = model.SupplierLocation;
        match.Status = model.Status;
        match.IsActive = model.IsActive;
        match.UpdatedOn = DateTime.UtcNow;
        match.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        match.IsActive = false;
        match.UpdatedOn = DateTime.UtcNow;
        match.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> SearchBuyers(string? q)
    {
        var query = Context.Buyers
            .AsNoTracking()
            .Where(b => b.IsActive == true);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(b => b.Name != null && b.Name.Contains(q));
        }

        var results = await query
            .OrderBy(b => b.Name)
            .Take(50)
            .Select(b => new { value = b.Id.ToString(), text = b.Name ?? "(unnamed buyer)" })
            .ToListAsync();

        return Json(results);
    }

    public async Task<IActionResult> SearchSuppliers(string? q)
    {
        var query = Context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive == true);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(s => s.Name != null && s.Name.Contains(q));
        }

        var results = await query
            .OrderBy(s => s.Name)
            .Take(50)
            .Select(s => new { value = s.Id.ToString(), text = s.Name ?? "(unnamed supplier)" })
            .ToListAsync();

        return Json(results);
    }

    public async Task<IActionResult> SearchBuyerLocations(string? q)
    {
        var results = await SearchLocationsAsync(q, isBuyer: true);
        return Json(results);
    }

    public async Task<IActionResult> SearchSupplierLocations(string? q)
    {
        var results = await SearchLocationsAsync(q, isBuyer: false);
        return Json(results);
    }

    public async Task<IActionResult> SearchSupplierProducts(int buyerSupplierId, string? q)
    {
        var match = await Context.BuyerSuppliers
            .AsNoTracking()
            .Where(m => m.Id == buyerSupplierId)
            .Select(m => new { m.Supplier, m.SupplierLocation })
            .FirstOrDefaultAsync();

        if (match is null)
        {
            return NotFound();
        }

        var assignedProductIds = Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product => product.BuyerSupplierId == buyerSupplierId)
            .Select(product => product.SupplierProduct);

        var query = Context.SupplierProducts
            .AsNoTracking()
            .Where(product =>
                product.IsActive
                && product.Supplier == match.Supplier
                && product.Location == match.SupplierLocation
                && !assignedProductIds.Contains(product.Id));

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(product =>
                (product.ProductNavigation != null && product.ProductNavigation.Name != null && product.ProductNavigation.Name.Contains(q))
                || (product.PackagingNavigation != null && product.PackagingNavigation.Type != null && product.PackagingNavigation.Type.Contains(q))
                || (product.OtherPackaging != null && product.OtherPackaging.Contains(q)));
        }

        var results = await query
            .OrderBy(product => product.ProductNavigation != null ? product.ProductNavigation.Name : null)
            .ThenBy(product => product.PackagingNavigation != null ? product.PackagingNavigation.Type : product.OtherPackaging)
            .Take(50)
            .Select(product => new SelectOption
            {
                Value = product.Id.ToString(),
                Text = ((product.ProductNavigation != null ? product.ProductNavigation.Name : null) ?? "(unnamed product)")
                    + (product.PackagingNavigation != null && product.PackagingNavigation.Type != null
                        ? " - " + product.PackagingNavigation.Type
                        : product.OtherPackaging != null ? " - " + product.OtherPackaging : string.Empty),
            })
            .ToListAsync();

        return Json(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(BuyerSupplierProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose a supplier product and enter a valid buyer price before saving.";
            return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
        }

        var matchExists = await Context.BuyerSuppliers.AnyAsync(m => m.Id == model.BuyerSupplierId);
        if (!matchExists)
        {
            return NotFound();
        }

        var duplicateAssignment = await Context.BuyerSupplierProducts.AnyAsync(product =>
            product.BuyerSupplierId == model.BuyerSupplierId
            && product.SupplierProduct == model.SupplierProduct);

        if (duplicateAssignment)
        {
            TempData["Error"] = "That supplier product is already assigned to this match.";
            return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
        }

        var assignment = new BuyerSupplierProduct
        {
            BuyerSupplierId = model.BuyerSupplierId,
            SupplierProduct = model.SupplierProduct,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.BuyerSupplierProducts.Add(assignment);
        await Context.SaveChangesAsync();

        Context.BuyerProductRates.Add(new BuyerProductRate
        {
            BuyerSupplierProductId = assignment.Id,
            Price = model.Price,
            EffectiveDate = model.EffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = User.GetLegacyUserId(),
            IsActive = true,
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProductRate(BuyerProductRateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid buyer rate before saving.";
            return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
        }

        var assignment = await Context.BuyerSupplierProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(product =>
                product.Id == model.BuyerSupplierProductId
                && product.BuyerSupplierId == model.BuyerSupplierId);

        if (assignment is null)
        {
            return NotFound();
        }

        var duplicateRate = await Context.BuyerProductRates.AnyAsync(rate =>
            rate.BuyerSupplierProductId == model.BuyerSupplierProductId
            && rate.EffectiveDate.HasValue
            && model.EffectiveDate.HasValue
            && rate.EffectiveDate.Value.Date == model.EffectiveDate.Value.Date);

        if (duplicateRate)
        {
            TempData["Error"] = "A buyer rate already exists for that effective date.";
            return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
        }

        var userId = User.GetLegacyUserId();
        Context.BuyerProductRates.Add(new BuyerProductRate
        {
            BuyerSupplierProductId = model.BuyerSupplierProductId,
            Price = model.Price,
            EffectiveDate = model.EffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = userId,
            IsActive = true,
        });
        AddBuyerProductHistory(model.BuyerSupplierProductId, oldPrice: null, model.Price, oldEffectiveDate: null, model.EffectiveDate, userId, "Add");
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.BuyerSupplierId });
    }

    public async Task<IActionResult> ProductDetails(int id)
    {
        var model = await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new BuyerSupplierProductDetailsViewModel
            {
                Id = product.Id,
                BuyerSupplierId = product.BuyerSupplierId ?? 0,
                BuyerName = product.BuyerSupplier != null && product.BuyerSupplier.BuyerNavigation != null
                    ? product.BuyerSupplier.BuyerNavigation.Name ?? string.Empty
                    : string.Empty,
                SupplierName = product.BuyerSupplier != null && product.BuyerSupplier.SupplierNavigation != null
                    ? product.BuyerSupplier.SupplierNavigation.Name ?? string.Empty
                    : string.Empty,
                ProductName = product.SupplierProductNavigation != null && product.SupplierProductNavigation.ProductNavigation != null
                    ? product.SupplierProductNavigation.ProductNavigation.Name ?? string.Empty
                    : string.Empty,
                PackagingName = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.PackagingNavigation != null
                        ? product.SupplierProductNavigation.PackagingNavigation.Type
                        : product.SupplierProductNavigation.OtherPackaging
                    : null,
                SupplierPrice = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.Price)
                        .FirstOrDefault()
                    : null,
                SupplierEffectiveDate = product.SupplierProductNavigation != null
                    ? product.SupplierProductNavigation.SupplierProductRates
                        .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                        .OrderByDescending(rate => rate.EffectiveDate)
                        .ThenByDescending(rate => rate.Id)
                        .Select(rate => rate.EffectiveDate)
                        .FirstOrDefault()
                    : null,
                NewRate = new BuyerProductRateFormViewModel
                {
                    BuyerSupplierProductId = product.Id,
                    BuyerSupplierId = product.BuyerSupplierId ?? 0,
                    EffectiveDate = DateTime.Today,
                },
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        model.Rates = await Context.BuyerProductRates
            .AsNoTracking()
            .Where(rate => rate.BuyerSupplierProductId == id)
            .OrderByDescending(rate => rate.EffectiveDate)
            .Select(rate => new BuyerProductRateListItemViewModel
            {
                Id = rate.Id,
                Price = rate.Price,
                EffectiveDate = rate.EffectiveDate,
                CreatedDate = rate.CreatedDate,
                UserName = rate.User != null ? rate.User.FirstName + " " + rate.User.LastName : null,
                IsActive = rate.IsActive,
            })
            .ToListAsync();

        model.RateHistory = await Context.BuyerProductHistories
            .AsNoTracking()
            .Where(history => history.BuyerSupplierProductId == id)
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

    public async Task<IActionResult> EditProductRate(int id)
    {
        var model = await Context.BuyerProductRates
            .AsNoTracking()
            .Where(rate => rate.Id == id)
            .Select(rate => new BuyerProductRateFormViewModel
            {
                Id = rate.Id,
                BuyerSupplierProductId = rate.BuyerSupplierProductId ?? 0,
                BuyerSupplierId = rate.BuyerSupplierProduct != null ? rate.BuyerSupplierProduct.BuyerSupplierId ?? 0 : 0,
                Price = rate.Price,
                EffectiveDate = rate.EffectiveDate,
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProductRate(int id, BuyerProductRateFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rate = await Context.BuyerProductRates.FindAsync(id);
        if (rate is null || rate.BuyerSupplierProductId != model.BuyerSupplierProductId)
        {
            return NotFound();
        }

        var duplicate = await Context.BuyerProductRates.AnyAsync(existing =>
            existing.Id != id
            && existing.BuyerSupplierProductId == model.BuyerSupplierProductId
            && existing.EffectiveDate.HasValue
            && model.EffectiveDate.HasValue
            && existing.EffectiveDate.Value.Date == model.EffectiveDate.Value.Date);

        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.EffectiveDate), "A buyer rate already exists for that effective date.");
            return View(model);
        }

        var oldPrice = rate.Price;
        var oldEffectiveDate = rate.EffectiveDate;
        var userId = User.GetLegacyUserId();

        rate.Price = model.Price;
        rate.EffectiveDate = model.EffectiveDate;
        rate.CreatedDate = DateTime.UtcNow;
        rate.UserId = userId;
        AddBuyerProductHistory(model.BuyerSupplierProductId, oldPrice, model.Price, oldEffectiveDate, model.EffectiveDate, userId, "Update");
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(ProductDetails), new { id = model.BuyerSupplierProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateProductRate(int id, int buyerSupplierProductId)
    {
        var rate = await Context.BuyerProductRates.FindAsync(id);
        if (rate is null || rate.BuyerSupplierProductId != buyerSupplierProductId)
        {
            return NotFound();
        }

        rate.IsActive = false;
        AddBuyerProductHistory(
            buyerSupplierProductId,
            rate.Price,
            null,
            rate.EffectiveDate,
            null,
            User.GetLegacyUserId(),
            "Delete");
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(ProductDetails), new { id = buyerSupplierProductId });
    }

    private async Task PopulateOptionsAsync(BuyerSupplierFormViewModel model)
    {
        model.BuyerOptions = await GetSelectedBuyerOptionsAsync(model.Buyer);
        model.SupplierOptions = await GetSelectedSupplierOptionsAsync(model.Supplier);
        model.BuyerLocationOptions = await GetSelectedLocationOptionsAsync(model.BuyerLocation);
        model.SupplierLocationOptions = await GetSelectedLocationOptionsAsync(model.SupplierLocation);
        model.StatusOptions = await Context.SupplierBuyerStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();
    }

    private void AddBuyerProductHistory(
        int buyerSupplierProductId,
        decimal? oldPrice,
        decimal? newPrice,
        DateTime? oldEffectiveDate,
        DateTime? newEffectiveDate,
        int? userId,
        string action)
    {
        Context.BuyerProductHistories.Add(new BuyerProductHistory
        {
            BuyerSupplierProductId = buyerSupplierProductId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            OldEffectiveDate = oldEffectiveDate,
            NewEffectiveDate = newEffectiveDate,
            CreatedDate = DateTime.UtcNow,
            UserId = userId,
            Action = action,
        });
    }

    private async Task ValidateSelectionsAsync(BuyerSupplierFormViewModel model)
    {
        if (model.Buyer is not null && model.BuyerLocation is not null)
        {
            var validBuyerLocation = await Context.Locations.AnyAsync(location =>
                location.Id == model.BuyerLocation &&
                location.ClientId == model.Buyer &&
                (location.IsBuyer == true || location.IsBuyer == null));
            if (!validBuyerLocation)
            {
                ModelState.AddModelError(nameof(model.BuyerLocation), "Choose a location for the selected buyer.");
            }
        }

        if (model.Supplier is not null && model.SupplierLocation is not null)
        {
            var validSupplierLocation = await Context.Locations.AnyAsync(location =>
                location.Id == model.SupplierLocation &&
                location.ClientId == model.Supplier &&
                (location.IsBuyer == false || location.IsBuyer == null));
            if (!validSupplierLocation)
            {
                ModelState.AddModelError(nameof(model.SupplierLocation), "Choose a location for the selected supplier.");
            }
        }
    }

    private async Task<List<SelectListItem>> GetSelectedBuyerOptionsAsync(int? selectedId)
    {
        if (selectedId is null)
        {
            return [];
        }

        return await Context.Buyers
            .AsNoTracking()
            .Where(b => b.Id == selectedId)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name, Selected = true })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetSelectedSupplierOptionsAsync(int? selectedId)
    {
        if (selectedId is null)
        {
            return [];
        }

        return await Context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == selectedId)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Selected = true })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetSelectedLocationOptionsAsync(int? selectedId)
    {
        if (selectedId is null)
        {
            return [];
        }

        var selectedLocations = await (
                from location in Context.Locations.AsNoTracking()
                join buyer in Context.Buyers.AsNoTracking() on location.ClientId equals buyer.Id into buyerJoin
                from buyer in buyerJoin.DefaultIfEmpty()
                join supplier in Context.Suppliers.AsNoTracking() on location.ClientId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                join state in Context.States.AsNoTracking() on location.State equals state.Id into stateJoin
                from state in stateJoin.DefaultIfEmpty()
                where location.Id == selectedId
                select new
                {
                    location.Id,
                    location.Location1,
                    location.City,
                    StateName = state != null ? state.StateName : null,
                    ClientName = location.IsBuyer == true
                        ? buyer != null ? buyer.Name : null
                        : supplier != null ? supplier.Name : null,
                })
            .OrderBy(l => l.Location1)
                .ToListAsync();

        return selectedLocations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = FormatLocationLabel(l.Location1, l.ClientName, l.City, l.StateName),
                Selected = true,
            })
                .ToList();
    }

    private async Task<List<SelectOption>> SearchLocationsAsync(string? q, bool isBuyer)
    {
        var query =
            from location in Context.Locations.AsNoTracking()
            join buyer in Context.Buyers.AsNoTracking() on location.ClientId equals buyer.Id into buyerJoin
            from buyer in buyerJoin.DefaultIfEmpty()
            join supplier in Context.Suppliers.AsNoTracking() on location.ClientId equals supplier.Id into supplierJoin
            from supplier in supplierJoin.DefaultIfEmpty()
            join state in Context.States.AsNoTracking() on location.State equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            join country in Context.Countries.AsNoTracking() on location.Country equals country.Id into countryJoin
            from country in countryJoin.DefaultIfEmpty()
            where location.IsActive && location.IsBuyer == isBuyer
            select new
            {
                location.Id,
                location.Location1,
                location.City,
                StateName = state != null ? state.StateName : null,
                CountryName = country != null ? country.CountryName : null,
                ClientName = isBuyer
                    ? buyer != null ? buyer.Name : null
                    : supplier != null ? supplier.Name : null,
            };

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(l =>
                (l.Location1 != null && l.Location1.Contains(q))
                || (l.ClientName != null && l.ClientName.Contains(q))
                || (l.City != null && l.City.Contains(q))
                || (l.StateName != null && l.StateName.Contains(q))
                || (l.CountryName != null && l.CountryName.Contains(q)));
        }

        var locations = await query
            .OrderBy(l => l.Location1)
            .Take(50)
            .ToListAsync();

        return locations
            .Select(l => new SelectOption
            {
                Value = l.Id.ToString(),
                Text = FormatLocationLabel(l.Location1, l.ClientName, l.City, l.StateName),
            })
            .ToList();
    }

    private static string FormatLocationLabel(string? location, string? client, string? city, string? state)
    {
        var label = string.IsNullOrWhiteSpace(location) ? "(unnamed location)" : location.Trim();

        if (!string.IsNullOrWhiteSpace(client))
        {
            label += " - " + client.Trim();
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            label += " - " + city.Trim();
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            label += ", " + state.Trim();
        }

        return label;
    }

    private sealed class SelectOption
    {
        public required string Value { get; init; }
        public required string Text { get; init; }
    }
}
