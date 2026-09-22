using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Extensions;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Shared;
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

    public override async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        var favoritesOnly = IsFavoritesOnlyRequest();
        if (favoritesOnly)
        {
            return await FavoriteIndex(search, sort, desc, page, pageSize);
        }

        if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(sort) || desc)
        {
            return await base.Index(search, sort, desc, page, pageSize);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;

        var totalCount = await Context.Suppliers.AsNoTracking().CountAsync();
        var pageSuppliers = Context.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
                from supplier in pageSuppliers
                join accountManager in Context.Users.AsNoTracking() on supplier.AccountManager equals accountManager.Id into accountManagerJoin
                from accountManager in accountManagerJoin.DefaultIfEmpty()
                orderby supplier.Id
                select new SupplierListItemViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name ?? string.Empty,
                    AccountManagerName = accountManager != null
                        ? accountManager.FirstName + " " + accountManager.LastName
                        : null,
                    IsActive = supplier.IsActive ?? false,
                })
            .ToListAsync();

        return View("Index", new PagedResult<SupplierListItemViewModel>
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

    private async Task<IActionResult> FavoriteIndex(string? search, string? sort, bool desc, int page, int pageSize)
    {
        var legacyUserId = User.GetLegacyUserId();
        if (legacyUserId is null)
        {
            return Forbid();
        }

        var query = GetBaseQuery().AsNoTracking()
            .Where(supplier => Context.Locations.Any(location =>
                location.ClientId == supplier.Id
                && location.IsActive
                && location.IsBuyer == false
                && location.Favorites.Any(favorite =>
                    favorite.UserId == legacyUserId
                    && !favorite.IsBuyer)));

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

        return View("Index", new PagedResult<SupplierListItemViewModel>
        {
            Items = items,
            Page = BuildPageInfo(page, pageSize, totalCount, search, resolvedSort, desc, favoritesOnly: true),
        });
    }

    private bool IsFavoritesOnlyRequest() =>
        string.Equals(Request.Query["favoritesOnly"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);

    private static PageInfo BuildPageInfo(
        int page,
        int pageSize,
        int totalCount,
        string? search,
        string? sort,
        bool desc,
        bool favoritesOnly) =>
        new()
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchTerm = search,
            SortColumn = sort,
            SortDescending = desc,
            AdditionalQueryParameters = favoritesOnly
                ? new Dictionary<string, string?> { ["favoritesOnly"] = "true" }
                : new Dictionary<string, string?>(),
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

        supplier.RecentLoads = await GetRecentLoadsAsync(id);

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

    public async Task<IActionResult> GetSubStatus(int id)
    {
        var subStatuses = await Context.SupplierStatuses
            .Where(status => status.ParentStatusId == id)
            .OrderBy(status => status.Status)
            .Select(status => new { id = status.Id, text = status.Status })
            .ToListAsync();

        return Json(subStatuses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int lid, int status, string? otherStatus)
    {
        var location = await Context.Locations.FindAsync(lid);
        if (location is null)
        {
            return NotFound();
        }

        if (location.IsBuyer != false)
        {
            return BadRequest("Location is not a supplier location.");
        }

        var statusExists = await Context.SupplierStatuses.AnyAsync(supplierStatus => supplierStatus.Id == status);
        if (!statusExists)
        {
            return BadRequest("Unknown supplier status.");
        }

        location.SupplierStatus = status;
        location.OtherStatus = string.IsNullOrWhiteSpace(otherStatus) ? null : otherStatus;
        location.UpdatedOn = DateTime.UtcNow;
        location.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return Json(new { success = true });
    }

    public async Task<IActionResult> Tracking(int? userId)
    {
        var resolvedUserId = userId ?? User.GetLegacyUserId();
        if (resolvedUserId is null)
        {
            return Forbid();
        }

        var user = await Context.Users
            .AsNoTracking()
            .Where(user => user.Id == resolvedUserId)
            .Select(user => new
            {
                user.Id,
                Name = user.FirstName == user.LastName ? user.FirstName : user.FirstName + " " + user.LastName,
            })
            .FirstOrDefaultAsync();
        if (user is null)
        {
            return NotFound();
        }

        var products = await Context.UserProducts
            .AsNoTracking()
            .Where(userProduct => userProduct.UserId == resolvedUserId)
            .OrderBy(userProduct => userProduct.OrderCount)
            .ThenBy(userProduct => userProduct.ProductNavigation.Name)
            .Select(userProduct => new SupplierTrackingProductViewModel
            {
                UserProductId = userProduct.Id,
                ProductId = userProduct.Product,
                ProductName = userProduct.ProductNavigation.Name ?? string.Empty,
                OrderCount = userProduct.OrderCount,
                Suppliers = userProduct.UserSuppliers
                    .OrderBy(userSupplier => userSupplier.OrderCount)
                    .ThenBy(userSupplier => userSupplier.Supplier.Name)
                    .Select(userSupplier => new SupplierTrackingSupplierViewModel
                    {
                        UserSupplierId = userSupplier.Id,
                        SupplierId = userSupplier.SupplierId,
                        SupplierName = userSupplier.Supplier.Name ?? string.Empty,
                        LocationId = userSupplier.LocationId,
                        LocationName = userSupplier.Location.Location1 ?? string.Empty,
                        OrderCount = userSupplier.OrderCount,
                        SupplierNote = userSupplier.SupplierNote,
                    })
                    .ToList(),
            })
            .ToListAsync();

        return View(new SupplierTrackingViewModel
        {
            UserId = user.Id,
            UserName = user.Name ?? string.Empty,
            UserOptions = await GetTrackingUserOptionsAsync(user.Id),
            Products = products,
        });
    }

    public async Task<IActionResult> AddTrackingProduct(int? userId)
    {
        var resolvedUserId = userId ?? User.GetLegacyUserId();
        if (resolvedUserId is null)
        {
            return Forbid();
        }

        return View(new SupplierTrackingProductFormViewModel
        {
            CurrentUserId = resolvedUserId.Value,
            ProductOptions = await GetTrackingProductOptionsAsync(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrackingProduct(SupplierTrackingProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ProductOptions = await GetTrackingProductOptionsAsync();
            return View(model);
        }

        var userExists = await Context.Users.AnyAsync(user => user.Id == model.CurrentUserId && user.IsActive == true);
        if (!userExists)
        {
            ModelState.AddModelError(string.Empty, "Choose an active tracking user.");
        }

        var productExists = await Context.Products.AnyAsync(product => product.Id == model.ProductId && product.IsActive == true);
        if (!productExists)
        {
            ModelState.AddModelError(nameof(model.ProductId), "Choose an active product.");
        }

        var duplicate = await Context.UserProducts.AnyAsync(userProduct =>
            userProduct.UserId == model.CurrentUserId
            && userProduct.Product == model.ProductId);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Product already exists for this tracking user.");
        }

        if (!ModelState.IsValid)
        {
            model.ProductOptions = await GetTrackingProductOptionsAsync();
            return View(model);
        }

        Context.UserProducts.Add(new Data.Models.UserProduct
        {
            UserId = model.CurrentUserId,
            Product = model.ProductId!.Value,
            OrderCount = 0,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Tracking), new { userId = model.CurrentUserId });
    }

    public async Task<IActionResult> AddTrackingSupplier(int userProductId)
    {
        var model = await BuildTrackingSupplierFormAsync(userProductId, null, null);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrackingSupplier(SupplierTrackingSupplierFormViewModel model)
    {
        var userProduct = await Context.UserProducts
            .AsNoTracking()
            .Where(product => product.Id == model.UserProductId)
            .Select(product => new { product.Id, product.UserId, product.Product })
            .FirstOrDefaultAsync();
        if (userProduct is null)
        {
            return NotFound();
        }

        model.CurrentUserId = userProduct.UserId;
        model.ProductId = userProduct.Product;

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingSupplierFormAsync(model.UserProductId, model.SupplierId, model.LocationId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        var locationIsValid = await Context.SupplierProducts.AnyAsync(supplierProduct =>
            supplierProduct.IsActive
            && supplierProduct.Product == userProduct.Product
            && supplierProduct.Supplier == model.SupplierId
            && supplierProduct.Location == model.LocationId
            && supplierProduct.SupplierNavigation != null
            && supplierProduct.SupplierNavigation.IsActive == true
            && supplierProduct.LocationNavigation != null
            && supplierProduct.LocationNavigation.IsActive
            && supplierProduct.LocationNavigation.IsBuyer == false);
        if (!locationIsValid)
        {
            ModelState.AddModelError(nameof(model.LocationId), "Choose an active supplier location for this product.");
        }

        var duplicate = await Context.UserSuppliers.AnyAsync(userSupplier =>
            userSupplier.UserProductId == model.UserProductId
            && userSupplier.SupplierId == model.SupplierId
            && userSupplier.LocationId == model.LocationId);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Supplier already exists for this tracking product.");
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingSupplierFormAsync(model.UserProductId, model.SupplierId, model.LocationId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        Context.UserSuppliers.Add(new Data.Models.UserSupplier
        {
            UserProductId = model.UserProductId,
            OrderCount = 0,
            SupplierId = model.SupplierId!.Value,
            LocationId = model.LocationId!.Value,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Tracking), new { userId = userProduct.UserId });
    }

    public async Task<IActionResult> GetTrackingSupplierLocations(int productId, int supplierId)
    {
        var locations = await GetTrackingSupplierLocationOptionsAsync(productId, supplierId, null);
        return Json(locations.Select(location => new { id = location.Value, text = location.Text }));
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

    private async Task<IEnumerable<SelectListItem>> GetTrackingUserOptionsAsync(int selectedId) =>
        await Context.Users
            .AsNoTracking()
            .Where(user => user.IsActive == true)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FirstName == user.LastName ? user.FirstName : user.FirstName + " " + user.LastName,
                Selected = user.Id == selectedId,
            })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetTrackingProductOptionsAsync(int? selectedId = null) =>
        await Context.Products
            .AsNoTracking()
            .Where(product => product.IsActive == true)
            .OrderBy(product => product.Name)
            .Select(product => new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = product.Name,
                Selected = selectedId == product.Id,
            })
            .ToListAsync();

    private async Task<SupplierTrackingSupplierFormViewModel?> BuildTrackingSupplierFormAsync(int userProductId, int? selectedSupplierId, int? selectedLocationId)
    {
        var userProduct = await Context.UserProducts
            .AsNoTracking()
            .Where(product => product.Id == userProductId)
            .Select(product => new
            {
                product.Id,
                product.UserId,
                product.Product,
                ProductName = product.ProductNavigation.Name,
            })
            .FirstOrDefaultAsync();
        if (userProduct is null)
        {
            return null;
        }

        return new SupplierTrackingSupplierFormViewModel
        {
            CurrentUserId = userProduct.UserId,
            UserProductId = userProduct.Id,
            ProductId = userProduct.Product,
            ProductName = userProduct.ProductName ?? string.Empty,
            SupplierId = selectedSupplierId,
            LocationId = selectedLocationId,
            SupplierOptions = await GetTrackingSupplierOptionsAsync(userProduct.Product, selectedSupplierId),
            LocationOptions = selectedSupplierId.HasValue
                ? await GetTrackingSupplierLocationOptionsAsync(userProduct.Product, selectedSupplierId.Value, selectedLocationId)
                : [],
        };
    }

    private async Task<IEnumerable<SelectListItem>> GetTrackingSupplierOptionsAsync(int productId, int? selectedId = null) =>
        await Context.SupplierProducts
            .AsNoTracking()
            .Where(supplierProduct =>
                supplierProduct.IsActive
                && supplierProduct.Product == productId
                && supplierProduct.SupplierNavigation != null
                && supplierProduct.SupplierNavigation.IsActive == true)
            .Select(supplierProduct => new
            {
                Id = supplierProduct.Supplier!.Value,
                Name = supplierProduct.SupplierNavigation!.Name ?? string.Empty,
            })
            .Distinct()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new SelectListItem
            {
                Value = supplier.Id.ToString(),
                Text = supplier.Name,
                Selected = selectedId == supplier.Id,
            })
            .ToListAsync();

    private async Task<List<SelectListItem>> GetTrackingSupplierLocationOptionsAsync(int productId, int supplierId, int? selectedId) =>
        await Context.SupplierProducts
            .AsNoTracking()
            .Where(supplierProduct =>
                supplierProduct.IsActive
                && supplierProduct.Product == productId
                && supplierProduct.Supplier == supplierId
                && supplierProduct.SupplierNavigation != null
                && supplierProduct.SupplierNavigation.IsActive == true
                && supplierProduct.LocationNavigation != null
                && supplierProduct.LocationNavigation.IsActive
                && supplierProduct.LocationNavigation.IsBuyer == false)
            .Select(supplierProduct => new
            {
                Id = supplierProduct.Location!.Value,
                Name = supplierProduct.LocationNavigation!.Location1 ?? string.Empty,
            })
            .Distinct()
            .OrderBy(location => location.Name)
            .Select(location => new SelectListItem
            {
                Value = location.Id.ToString(),
                Text = location.Name,
                Selected = selectedId == location.Id,
            })
            .ToListAsync();

    private async Task<IReadOnlyList<RecentLoadListItemViewModel>> GetRecentLoadsAsync(int supplierId)
    {
        var loads = await Context.Loads
            .AsNoTracking()
            .Where(load => load.Supplier == supplierId && load.IsActive == true)
            .Include(load => load.LoadStatusNavigation)
            .Include(load => load.BuyerNavigation)
            .Include(load => load.SupplierNavigation)
            .Include(load => load.BuyerLocationNavigation)
            .Include(load => load.SupplierLocationNavigation)
            .Include(load => load.LoadProducts)
                .ThenInclude(loadProduct => loadProduct.ProductNavigation)
                    .ThenInclude(product => product.ProductNavigation)
            .OrderByDescending(load => load.ShipmentDate)
            .ThenByDescending(load => load.Id)
            .Take(5)
            .ToListAsync();

        return loads
            .Select(load => new RecentLoadListItemViewModel
            {
                Id = load.Id,
                ShipmentDate = load.ShipmentDate,
                StatusName = load.LoadStatusNavigation?.Status,
                BuyerName = load.BuyerNavigation?.Name,
                SupplierName = load.SupplierNavigation?.Name,
                BuyerLocationName = load.BuyerLocationNavigation?.Location1,
                SupplierLocationName = load.SupplierLocationNavigation?.Location1,
                Products = string.Join(", ", load.LoadProducts
                    .Select(loadProduct => loadProduct.ProductNavigation?.ProductNavigation?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()),
            })
            .ToList();
    }
}
