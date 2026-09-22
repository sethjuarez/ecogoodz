using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Extensions;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Load;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class LoadController : PagedListController<Data.Models.Load, LoadListItemViewModel>
{
    public LoadController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.Load> GetBaseQuery() =>
        Context.Loads
            .Include(l => l.BuyerNavigation)
            .Include(l => l.SupplierNavigation)
            .Include(l => l.BuyerLocationNavigation)
            .Include(l => l.SupplierLocationNavigation)
            .Include(l => l.LoadStatusNavigation);

    protected override IQueryable<Data.Models.Load> ApplySearch(IQueryable<Data.Models.Load> query, string searchTerm) =>
        query.Where(l =>
            (l.BuyerNavigation != null && l.BuyerNavigation.Name != null && l.BuyerNavigation.Name.Contains(searchTerm))
            || (l.SupplierNavigation != null && l.SupplierNavigation.Name != null && l.SupplierNavigation.Name.Contains(searchTerm))
            || (l.BuyerRef != null && l.BuyerRef.Contains(searchTerm))
            || (l.SupplierRef != null && l.SupplierRef.Contains(searchTerm))
            || (l.Container != null && l.Container.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.Load, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.Load, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["buyer"] = l => l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
            ["supplier"] = l => l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
            ["status"] = l => l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
            ["shipmentDate"] = l => l.ShipmentDate,
            ["active"] = l => l.IsActive,
        };

    protected override string DefaultSortColumn => "shipmentDate";

    protected override Expression<Func<Data.Models.Load, LoadListItemViewModel>> ProjectionExpression =>
        l => new LoadListItemViewModel
        {
            Id = l.Id,
            BuyerId = l.Buyer,
            SupplierId = l.Supplier,
            BuyerLocationId = l.BuyerLocation,
            SupplierLocationId = l.SupplierLocation,
            BuyerName = l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
            SupplierName = l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
            BuyerLocationName = l.BuyerLocationNavigation != null ? l.BuyerLocationNavigation.Location1 : null,
            SupplierLocationName = l.SupplierLocationNavigation != null ? l.SupplierLocationNavigation.Location1 : null,
            StatusName = l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
            ShipmentDate = l.ShipmentDate,
            IsActive = l.IsActive ?? false,
        };

    public override async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        var buyerId = ReadIntQuery("buyerId");
        var supplierId = ReadIntQuery("supplierId");
        var buyerLocationId = ReadIntQuery("buyerLocationId");
        var supplierLocationId = ReadIntQuery("supplierLocationId");
        var locationId = ReadIntQuery("locationId");
        var hasScope = buyerId.HasValue
            || supplierId.HasValue
            || buyerLocationId.HasValue
            || supplierLocationId.HasValue
            || locationId.HasValue;
        if (!hasScope)
        {
            return await base.Index(search, sort, desc, page, pageSize);
        }

        var query = ApplyScope(GetBaseQuery().AsNoTracking(), buyerId, supplierId, buyerLocationId, supplierLocationId, locationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;

        var totalCount = await query.CountAsync();
        var sorted = query.ApplySort(sort, desc, SortColumns, DefaultSortColumn, out var resolvedSort);
        var items = await sorted
            .Select(ProjectionExpression)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View("Index", new PagedResult<LoadListItemViewModel>
        {
            Items = items,
            Page = new PageInfo
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search,
                SortColumn = resolvedSort,
                SortDescending = desc,
                AdditionalQueryParameters = BuildScopeParameters(
                    buyerId,
                    supplierId,
                    buyerLocationId,
                    supplierLocationId,
                    locationId),
            },
        });
    }

    private static IQueryable<Data.Models.Load> ApplyScope(
        IQueryable<Data.Models.Load> query,
        int? buyerId,
        int? supplierId,
        int? buyerLocationId,
        int? supplierLocationId,
        int? locationId)
    {
        if (buyerId.HasValue)
        {
            query = query.Where(load => load.Buyer == buyerId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(load => load.Supplier == supplierId.Value);
        }

        if (buyerLocationId.HasValue)
        {
            query = query.Where(load => load.BuyerLocation == buyerLocationId.Value);
        }

        if (supplierLocationId.HasValue)
        {
            query = query.Where(load => load.SupplierLocation == supplierLocationId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(load =>
                load.BuyerLocation == locationId.Value
                || load.SupplierLocation == locationId.Value);
        }

        return query;
    }

    private static IReadOnlyDictionary<string, string?> BuildScopeParameters(
        int? buyerId,
        int? supplierId,
        int? buyerLocationId,
        int? supplierLocationId,
        int? locationId)
    {
        var parameters = new Dictionary<string, string?>();
        if (buyerId.HasValue)
        {
            parameters["buyerId"] = buyerId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (supplierId.HasValue)
        {
            parameters["supplierId"] = supplierId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (buyerLocationId.HasValue)
        {
            parameters["buyerLocationId"] = buyerLocationId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (supplierLocationId.HasValue)
        {
            parameters["supplierLocationId"] = supplierLocationId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (locationId.HasValue)
        {
            parameters["locationId"] = locationId.Value.ToString(CultureInfo.InvariantCulture);
        }

        return parameters;
    }

    private int? ReadIntQuery(string key) =>
        HttpContext?.Request.Query.TryGetValue(key, out var rawValue) == true
        && int.TryParse(rawValue.ToString(), CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    public async Task<IActionResult> Details(int id)
    {
        var load = await Context.Loads
            .Include(l => l.BuyerNavigation)
            .Include(l => l.SupplierNavigation)
            .Include(l => l.LoadStatusNavigation)
            .Where(l => l.Id == id)
            .Select(l => new LoadDetailsViewModel
            {
                Id = l.Id,
                BuyerId = l.Buyer,
                BuyerName = l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
                BuyerLocationId = l.BuyerLocation,
                BuyerLocationName = l.BuyerLocationNavigation != null ? l.BuyerLocationNavigation.Location1 : null,
                SupplierId = l.Supplier,
                SupplierName = l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
                SupplierLocationId = l.SupplierLocation,
                SupplierLocationName = l.SupplierLocationNavigation != null ? l.SupplierLocationNavigation.Location1 : null,
                StatusName = l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
                ShipmentDate = l.ShipmentDate,
                Container = l.Container,
                BuyerRef = l.BuyerRef,
                SupplierRef = l.SupplierRef,
                FreightCarrier = l.FreightCarrier,
                FreightInvoice = l.FreightInvoice,
                FreightAmountQuoted = l.FreightAmountQuoted,
                FreightAmountBilled = l.FreightAmountBilled,
                BuyerInvoice = l.BuyerInvoice,
                BuyerInvoiceAmount = l.BuyerInvoiceAmount,
                BuyerInvoiceDate = l.BuyerInvoiceDate,
                SupplierInvoice = l.SupplierInvoice,
                SupplierInvoiceAmount = l.SupplierInvoiceAmount,
                SupplierInvoiceDate = l.SupplierInvoiceDate,
                BuyerNotes = l.BuyerNotes,
                SupplierNotes = l.SupplierNotes,
                FreightNotes = l.FreightNotes,
                IsComplete = l.IsComplete ?? false,
                IsActive = l.IsActive ?? false,
                CreateOn = l.CreateOn,
                UpdatedOn = l.UpdatedOn,
                ProductLineCount = l.LoadProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (load is null)
        {
            return NotFound();
        }

        load.ProductLines = await Context.LoadProducts
            .AsNoTracking()
            .Where(lp => lp.Load == id)
            .Select(lp => new LoadProductLineViewModel
            {
                SupplierProductId = lp.Product,
                SupplierName = lp.ProductNavigation.SupplierNavigation != null ? lp.ProductNavigation.SupplierNavigation.Name : null,
                ProductName = lp.ProductNavigation.ProductNavigation != null ? lp.ProductNavigation.ProductNavigation.Name : null,
                PackagingName = lp.ProductNavigation.PackagingNavigation != null
                    ? lp.ProductNavigation.PackagingNavigation.Type
                    : lp.ProductNavigation.OtherPackaging,
                CurrentPrice = lp.ProductNavigation.SupplierProductRates
                    .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveDate)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => rate.Price)
                    .FirstOrDefault(),
                EffectiveDate = lp.ProductNavigation.SupplierProductRates
                    .Where(rate => rate.IsActive && (rate.EffectiveDate == null || rate.EffectiveDate <= DateTime.Today))
                    .OrderByDescending(rate => rate.EffectiveDate)
                    .ThenByDescending(rate => rate.Id)
                    .Select(rate => rate.EffectiveDate)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return View(load);
    }

    public async Task<IActionResult> Export(string? search, string? sort, bool desc = false)
    {
        var buyerId = ReadIntQuery("buyerId");
        var supplierId = ReadIntQuery("supplierId");
        var buyerLocationId = ReadIntQuery("buyerLocationId");
        var supplierLocationId = ReadIntQuery("supplierLocationId");
        var locationId = ReadIntQuery("locationId");
        var hasScope = buyerId.HasValue
            || supplierId.HasValue
            || buyerLocationId.HasValue
            || supplierLocationId.HasValue
            || locationId.HasValue;

        if (!hasScope && string.IsNullOrWhiteSpace(search))
        {
            return BadRequest("Export requires a search term or scoped filter.");
        }

        IQueryable<Data.Models.Load> query = GetBaseQuery()
            .AsNoTracking();

        query = ApplyScope(
            query,
            buyerId,
            supplierId,
            buyerLocationId,
            supplierLocationId,
            locationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        var loads = await query
            .ApplySort(sort, desc, SortColumns, DefaultSortColumn, out _)
            .Select(load => new LoadExportRow
            {
                Id = load.Id,
                StatusName = load.LoadStatusNavigation != null ? load.LoadStatusNavigation.Status : null,
                BuyerName = load.BuyerNavigation != null ? load.BuyerNavigation.Name : null,
                BuyerLocationName = load.BuyerLocationNavigation != null ? load.BuyerLocationNavigation.Location1 : null,
                SupplierName = load.SupplierNavigation != null ? load.SupplierNavigation.Name : null,
                SupplierLocationName = load.SupplierLocationNavigation != null ? load.SupplierLocationNavigation.Location1 : null,
                ShipmentDate = load.ShipmentDate,
                BookingDate = load.BookingDate,
                BuyerRef = load.BuyerRef,
                SupplierRef = load.SupplierRef,
                Container = load.Container,
                BuyerInvoice = load.BuyerInvoice,
                BuyerInvoiceAmount = load.BuyerInvoiceAmount,
                SupplierInvoice = load.SupplierInvoice,
                SupplierInvoiceAmount = load.SupplierInvoiceAmount,
                FreightCarrier = load.FreightCarrier,
                FreightInvoice = load.FreightInvoice,
                FreightAmountQuoted = load.FreightAmountQuoted,
                FreightAmountBilled = load.FreightAmountBilled,
                IsActive = load.IsActive ?? false,
            })
            .ToListAsync();

        var productsLookup = new Dictionary<int, string>();
        if (loads.Count > 0)
        {
            var loadIds = loads.Select(load => load.Id).ToList();
            var productNamesByLoad = await Context.LoadProducts
                .AsNoTracking()
                .Where(loadProduct => loadIds.Contains(loadProduct.Load))
                .Select(loadProduct => new
                {
                    LoadId = loadProduct.Load,
                    Name = loadProduct.ProductNavigation.ProductNavigation != null
                        ? loadProduct.ProductNavigation.ProductNavigation.Name
                        : null,
                })
                .Where(product => !string.IsNullOrWhiteSpace(product.Name))
                .ToListAsync();

            productsLookup = productNamesByLoad
                .GroupBy(product => product.LoadId)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(", ", group
                        .Select(product => product.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct()));
        }

        var csv = new StringBuilder();
        AppendCsvRow(csv,
            "Id",
            "Status",
            "Buyer",
            "Buyer Location",
            "Supplier",
            "Supplier Location",
            "Products",
            "Shipment Date",
            "Booking Date",
            "Buyer Ref",
            "Supplier Ref",
            "Container",
            "Buyer Invoice",
            "Buyer Invoice Amount",
            "Supplier Invoice",
            "Supplier Invoice Amount",
            "Freight Carrier",
            "Freight Invoice",
            "Freight Quoted",
            "Freight Billed",
            "Active");

        foreach (var load in loads)
        {
            AppendCsvRow(csv,
                load.Id.ToString(),
                load.StatusName,
                load.BuyerName,
                load.BuyerLocationName,
                load.SupplierName,
                load.SupplierLocationName,
                productsLookup.GetValueOrDefault(load.Id, string.Empty),
                FormatDate(load.ShipmentDate),
                FormatDate(load.BookingDate),
                load.BuyerRef,
                load.SupplierRef,
                load.Container,
                load.BuyerInvoice,
                FormatDecimal(load.BuyerInvoiceAmount),
                load.SupplierInvoice,
                FormatDecimal(load.SupplierInvoiceAmount),
                load.FreightCarrier,
                load.FreightInvoice,
                FormatDecimal(load.FreightAmountQuoted),
                FormatDecimal(load.FreightAmountBilled),
                load.IsActive ? "Yes" : "No");
        }

        var csvContent = csv.ToString();
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, bytes, preamble.Length, content.Length);

        return File(bytes, "text/csv", "loads.csv");
    }

    private sealed class LoadExportRow
    {
        public int Id { get; set; }
        public string? StatusName { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerLocationName { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierLocationName { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public DateTime? BookingDate { get; set; }
        public string? BuyerRef { get; set; }
        public string? SupplierRef { get; set; }
        public string? Container { get; set; }
        public string? BuyerInvoice { get; set; }
        public decimal? BuyerInvoiceAmount { get; set; }
        public string? SupplierInvoice { get; set; }
        public decimal? SupplierInvoiceAmount { get; set; }
        public string? FreightCarrier { get; set; }
        public string? FreightInvoice { get; set; }
        public decimal? FreightAmountQuoted { get; set; }
        public decimal? FreightAmountBilled { get; set; }
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> GetLoadBySupplierAcctMgr(int id)
    {
        var supplier = await Context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Id == id)
            .Select(supplier => new
            {
                supplier.Id,
                supplier.AccountManager,
            })
            .FirstOrDefaultAsync();
        if (supplier is null)
        {
            return NotFound();
        }

        var locationList = await Context.Locations
            .AsNoTracking()
            .Where(location =>
                location.ClientId == supplier.Id
                && location.IsActive
                && (location.IsBuyer == false || location.IsBuyer == null)
                && location.SupplierStatus != null
                && location.SupplierStatusNavigation != null
                && location.SupplierStatusNavigation.ParentStatusId == 2)
            .OrderBy(location => location.Location1)
            .Select(location => new { id = location.Id, text = location.Location1 })
            .ToListAsync();

        var supplierAccountManagerList = await Context.Users
            .AsNoTracking()
            .Where(user => supplier.AccountManager != null && user.Id == supplier.AccountManager)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new
            {
                id = user.Id,
                text = user.FirstName == user.LastName ? user.FirstName : user.FirstName + " " + user.LastName,
            })
            .ToListAsync();

        return Json(new
        {
            locationList,
            supplierAccountManagerList,
            accountmgrID = supplier.AccountManager,
        });
    }

    public async Task<IActionResult> Create()
    {
        var model = new LoadFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoadFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        await ValidateSelectionsAsync(model, loadId: null);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var load = new Data.Models.Load
        {
            Buyer = model.Buyer,
            Supplier = model.Supplier,
            BuyerLocation = model.BuyerLocation,
            SupplierLocation = model.SupplierLocation,
            LoadStatus = model.LoadStatus,
            ShipmentDate = model.ShipmentDate,
            Container = model.Container,
            BuyerRef = model.BuyerRef,
            SupplierRef = model.SupplierRef,
            FreightCarrier = model.FreightCarrier,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        foreach (var supplierProductId in model.SupplierProductIds.Distinct())
        {
            load.LoadProducts.Add(new LoadProduct { Product = supplierProductId });
        }

        Context.Loads.Add(load);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        var model = new LoadFormViewModel
        {
            Id = load.Id,
            Buyer = load.Buyer,
            Supplier = load.Supplier,
            BuyerLocation = load.BuyerLocation,
            SupplierLocation = load.SupplierLocation,
            LoadStatus = load.LoadStatus,
            ShipmentDate = load.ShipmentDate,
            Container = load.Container,
            BuyerRef = load.BuyerRef,
            SupplierRef = load.SupplierRef,
            FreightCarrier = load.FreightCarrier,
            IsActive = load.IsActive ?? false,
            SupplierProductIds = await Context.LoadProducts
                .Where(lp => lp.Load == load.Id)
                .Select(lp => lp.Product)
                .ToListAsync(),
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LoadFormViewModel model)
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

        await ValidateSelectionsAsync(model, id);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        load.Buyer = model.Buyer;
        load.Supplier = model.Supplier;
        load.BuyerLocation = model.BuyerLocation;
        load.SupplierLocation = model.SupplierLocation;
        load.LoadStatus = model.LoadStatus;
        load.ShipmentDate = model.ShipmentDate;
        load.Container = model.Container;
        load.BuyerRef = model.BuyerRef;
        load.SupplierRef = model.SupplierRef;
        load.FreightCarrier = model.FreightCarrier;
        load.IsActive = model.IsActive;
        load.UpdatedOn = DateTime.UtcNow;
        load.UpdatedBy = User.GetLegacyUserId();

        if (Context.Database.IsRelational())
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();
            await SyncLoadProductsAsync(load.Id, model.SupplierProductIds);
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        else
        {
            await SyncLoadProductsAsync(load.Id, model.SupplierProductIds);
            await Context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // Soft delete only, matching the legacy IsActive convention - loads are
    // referenced by gross profit projections and history throughout the schema.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        load.IsActive = false;
        load.UpdatedOn = DateTime.UtcNow;
        load.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(LoadFormViewModel model)
    {
        model.BuyerOptions = await GetBuyerOptionsAsync();
        model.SupplierOptions = await GetSupplierOptionsAsync();
        model.BuyerLocationOptions = await GetLocationOptionsAsync(isBuyer: true, model.Buyer, model.BuyerLocation);
        model.SupplierLocationOptions = await GetLocationOptionsAsync(isBuyer: false, model.Supplier, model.SupplierLocation);
        model.SupplierProductOptions = await GetSupplierProductOptionsAsync(model.Supplier, model.SupplierProductIds);
        model.StatusOptions = await GetStatusOptionsAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetBuyerOptionsAsync() =>
        await Context.Buyers
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetSupplierOptionsAsync() =>
        await Context.Suppliers
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetLocationOptionsAsync(bool isBuyer, int? clientId, int? selectedId)
    {
        var locations = Context.Locations
            .Where(l => ((clientId == null || l.ClientId == clientId)
                    && l.IsActive
                    && (l.IsBuyer == isBuyer || l.IsBuyer == null))
                || l.Id == selectedId);

        if (isBuyer)
        {
            return await (
                    from location in locations
                    join buyer in Context.Buyers on location.ClientId equals buyer.Id into buyerJoin
                    from buyer in buyerJoin.DefaultIfEmpty()
                    orderby buyer.Name, location.Location1
                    select new SelectListItem
                    {
                        Value = location.Id.ToString(),
                        Text = (buyer.Name ?? "Buyer") + " - " + location.Location1,
                    })
                .ToListAsync();
        }

        return await (
                from location in locations
                join supplier in Context.Suppliers on location.ClientId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                orderby supplier.Name, location.Location1
                select new SelectListItem
                {
                    Value = location.Id.ToString(),
                    Text = (supplier.Name ?? "Supplier") + " - " + location.Location1,
                })
            .ToListAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetSupplierProductOptionsAsync(int? supplierId, IReadOnlyCollection<int> selectedIds) =>
        await Context.SupplierProducts
            .Where(sp => ((supplierId == null || sp.Supplier == supplierId) && sp.IsActive) || selectedIds.Contains(sp.Id))
            .OrderBy(sp => sp.SupplierNavigation!.Name)
            .ThenBy(sp => sp.ProductNavigation!.Name)
            .Select(sp => new SelectListItem
            {
                Value = sp.Id.ToString(),
                Text = (sp.SupplierNavigation != null ? sp.SupplierNavigation.Name : "Supplier")
                    + " - "
                    + (sp.ProductNavigation != null ? sp.ProductNavigation.Name : "Product")
                    + (sp.PackagingNavigation != null ? " (" + sp.PackagingNavigation.Type + ")" : sp.OtherPackaging != null ? " (" + sp.OtherPackaging + ")" : string.Empty),
            })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetStatusOptionsAsync() =>
        await Context.LoadStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();

    private static void AppendCsvRow(StringBuilder csv, params string?[] values)
    {
        csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        return value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static string? FormatDate(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatDecimal(decimal? value) => value?.ToString("0.####", CultureInfo.InvariantCulture);

    private async Task ValidateSelectionsAsync(LoadFormViewModel model, int? loadId)
    {
        if (model.BuyerLocation is not null && model.Buyer is not null)
        {
            var validBuyerLocation = await Context.Locations.AnyAsync(l =>
                l.Id == model.BuyerLocation &&
                l.ClientId == model.Buyer &&
                (l.IsBuyer == true || l.IsBuyer == null));
            if (!validBuyerLocation)
            {
                ModelState.AddModelError(nameof(model.BuyerLocation), "Choose a location for the selected buyer.");
            }
        }

        if (model.SupplierLocation is not null && model.Supplier is not null)
        {
            var validSupplierLocation = await Context.Locations.AnyAsync(l =>
                l.Id == model.SupplierLocation &&
                l.ClientId == model.Supplier &&
                (l.IsBuyer == false || l.IsBuyer == null));
            if (!validSupplierLocation)
            {
                ModelState.AddModelError(nameof(model.SupplierLocation), "Choose a location for the selected supplier.");
            }
        }

        if (model.SupplierProductIds.Count > 0 && model.Supplier is not null)
        {
            var distinctIds = model.SupplierProductIds.Distinct().ToList();
            var storedSupplier = loadId is null
                ? model.Supplier
                : await Context.Loads
                    .Where(load => load.Id == loadId)
                    .Select(load => load.Supplier)
                    .FirstOrDefaultAsync();
            var existingIds = loadId is null || storedSupplier != model.Supplier
                ? new List<int>()
                : await Context.LoadProducts
                    .Where(lp => lp.Load == loadId)
                    .Select(lp => lp.Product)
                    .ToListAsync();
            var newIds = distinctIds.Except(existingIds).ToList();
            var validCount = await Context.SupplierProducts.CountAsync(sp =>
                newIds.Contains(sp.Id) &&
                sp.Supplier == model.Supplier &&
                sp.IsActive);

            if (validCount != newIds.Count)
            {
                ModelState.AddModelError(nameof(model.SupplierProductIds), "Choose products offered by the selected supplier.");
            }
        }
    }

    private async Task SyncLoadProductsAsync(int loadId, IEnumerable<int> supplierProductIds)
    {
        var selectedIds = supplierProductIds.Distinct().ToHashSet();
        var existingRows = await Context.LoadProducts
            .Where(lp => lp.Load == loadId)
            .ToListAsync();

        Context.LoadProducts.RemoveRange(existingRows.Where(lp => !selectedIds.Contains(lp.Product)));

        var existingIds = existingRows.Select(lp => lp.Product).ToHashSet();
        foreach (var supplierProductId in selectedIds.Except(existingIds))
        {
            Context.LoadProducts.Add(new LoadProduct
            {
                Load = loadId,
                Product = supplierProductId,
            });
        }
    }
}
