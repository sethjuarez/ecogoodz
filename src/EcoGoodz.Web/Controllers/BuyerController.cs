using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Extensions;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Buyer;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class BuyerController : PagedListController<Data.Models.Buyer, BuyerListItemViewModel>
{
    public BuyerController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.Buyer> GetBaseQuery() =>
        Context.Buyers.Include(b => b.AccountManagerNavigation);

    protected override IQueryable<Data.Models.Buyer> ApplySearch(IQueryable<Data.Models.Buyer> query, string searchTerm) =>
        query.Where(b =>
            (b.Name != null && b.Name.Contains(searchTerm))
            || (b.AccountManagerNavigation != null && (
                (b.AccountManagerNavigation.FirstName != null && b.AccountManagerNavigation.FirstName.Contains(searchTerm))
                || (b.AccountManagerNavigation.LastName != null && b.AccountManagerNavigation.LastName.Contains(searchTerm)))));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.Buyer, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.Buyer, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = b => EF.Property<string>(b, "NameSort"),
            ["accountManager"] = b => b.AccountManagerNavigation != null ? b.AccountManagerNavigation.FirstName : null,
            ["active"] = b => b.IsActive,
        };

    protected override string DefaultSortColumn => "name";

    protected override Expression<Func<Data.Models.Buyer, BuyerListItemViewModel>> ProjectionExpression =>
        b => new BuyerListItemViewModel
        {
            Id = b.Id,
            Name = b.Name ?? string.Empty,
            AccountManagerName = b.AccountManagerNavigation != null
                ? b.AccountManagerNavigation.FirstName + " " + b.AccountManagerNavigation.LastName
                : null,
            IsActive = b.IsActive ?? false,
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

        var totalCount = await Context.Buyers.AsNoTracking().CountAsync();
        var pageBuyers = Context.Buyers
            .AsNoTracking()
            .OrderBy(buyer => buyer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
                from buyer in pageBuyers
                join accountManager in Context.Users.AsNoTracking() on buyer.AccountManager equals accountManager.Id into accountManagerJoin
                from accountManager in accountManagerJoin.DefaultIfEmpty()
                orderby buyer.Id
                select new BuyerListItemViewModel
                {
                    Id = buyer.Id,
                    Name = buyer.Name ?? string.Empty,
                    AccountManagerName = accountManager != null
                        ? accountManager.FirstName + " " + accountManager.LastName
                        : null,
                    IsActive = buyer.IsActive ?? false,
                })
            .ToListAsync();

        return View("Index", new PagedResult<BuyerListItemViewModel>
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
            .Where(buyer => Context.Locations.Any(location =>
                location.ClientId == buyer.Id
                && location.IsActive
                && location.IsBuyer == true
                && location.Favorites.Any(favorite =>
                    favorite.UserId == legacyUserId
                    && favorite.IsBuyer)));

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

        return View("Index", new PagedResult<BuyerListItemViewModel>
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
        var buyer = await Context.Buyers
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

        buyer.RecentLoads = await GetRecentLoadsAsync(id);

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

        Context.Buyers.Add(buyer);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var buyer = await Context.Buyers.FindAsync(id);
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

        var buyer = await Context.Buyers.FindAsync(id);
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

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Soft delete only, matching the legacy IsActive convention - buyers are
    // referenced by loads/history throughout the schema and are never hard-deleted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var buyer = await Context.Buyers.FindAsync(id);
        if (buyer is null)
        {
            return NotFound();
        }

        buyer.IsActive = false;
        buyer.UpdatedOn = DateTime.UtcNow;
        buyer.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GetSubStatus(int id)
    {
        var subStatuses = await Context.BuyerStatuses
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

        if (location.IsBuyer != true)
        {
            return BadRequest("Location is not a buyer location.");
        }

        var statusExists = await Context.BuyerStatuses.AnyAsync(buyerStatus => buyerStatus.Id == status);
        if (!statusExists)
        {
            return BadRequest("Unknown buyer status.");
        }

        location.BuyerStatus = status;
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
            .Where(user => user.Id == resolvedUserId && user.IsActive == true)
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

        var buyers = await Context.UserBuyers
            .AsNoTracking()
            .Where(userBuyer => userBuyer.UserId == resolvedUserId)
            .OrderBy(userBuyer => userBuyer.OrderCount)
            .ThenBy(userBuyer => userBuyer.Buyer.Name)
            .Select(userBuyer => new BuyerTrackingBuyerViewModel
            {
                UserBuyerId = userBuyer.Id,
                BuyerId = userBuyer.BuyerId,
                BuyerName = userBuyer.Buyer.Name ?? string.Empty,
                LocationId = userBuyer.LocationId,
                LocationName = userBuyer.Location.Location1 ?? string.Empty,
                OrderCount = userBuyer.OrderCount,
                Products = userBuyer.BuyerTrackingProducts
                    .OrderBy(product => product.Order)
                    .ThenBy(product => product.Product.Name)
                    .Select(product => new BuyerTrackingProductViewModel
                    {
                        BuyerTrackingProductId = product.Id,
                        ProductId = product.ProductId,
                        ProductName = product.Product.Name ?? string.Empty,
                        OrderCount = product.Order,
                        Suppliers = product.BuyerTrackingProductSuppliers
                            .OrderBy(supplier => supplier.OrderCount)
                            .ThenBy(supplier => supplier.Supplier.Name)
                            .Select(supplier => new BuyerTrackingSupplierViewModel
                            {
                                BuyerTrackingProductSupplierId = supplier.Id,
                                SupplierId = supplier.SupplierId,
                                SupplierName = supplier.Supplier.Name ?? string.Empty,
                                LocationId = supplier.LocationId,
                                LocationName = supplier.Location.Location1 ?? string.Empty,
                                OrderCount = supplier.OrderCount,
                                SupplierNote = supplier.SupplierNote,
                            })
                            .ToList(),
                    })
                    .ToList(),
            })
            .ToListAsync();

        return View(new BuyerTrackingViewModel
        {
            UserId = user.Id,
            UserName = user.Name ?? string.Empty,
            UserOptions = await GetTrackingUserOptionsAsync(user.Id),
            Buyers = buyers,
        });
    }

    public async Task<IActionResult> AddTrackingBuyer(int? userId)
    {
        var resolvedUserId = userId ?? User.GetLegacyUserId();
        if (resolvedUserId is null)
        {
            return Forbid();
        }

        return View(new BuyerTrackingBuyerFormViewModel
        {
            CurrentUserId = resolvedUserId.Value,
            BuyerOptions = await GetTrackingBuyerOptionsAsync(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrackingBuyer(BuyerTrackingBuyerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.BuyerOptions = await GetTrackingBuyerOptionsAsync(model.BuyerId);
            model.LocationOptions = model.BuyerId.HasValue ? await GetTrackingBuyerLocationOptionsAsync(model.BuyerId.Value, model.LocationId) : [];
            return View(model);
        }

        var userExists = await Context.Users.AnyAsync(user => user.Id == model.CurrentUserId && user.IsActive == true);
        if (!userExists)
        {
            ModelState.AddModelError(string.Empty, "Choose an active tracking user.");
        }

        var locationIsValid = await Context.Locations.AnyAsync(location =>
            location.Id == model.LocationId
            && location.ClientId == model.BuyerId
            && location.IsActive
            && location.IsBuyer == true
            && Context.Buyers.Any(buyer => buyer.Id == model.BuyerId && buyer.IsActive == true));
        if (!locationIsValid)
        {
            ModelState.AddModelError(nameof(model.LocationId), "Choose an active buyer location.");
        }

        var duplicate = await Context.UserBuyers.AnyAsync(userBuyer =>
            userBuyer.UserId == model.CurrentUserId
            && userBuyer.BuyerId == model.BuyerId
            && userBuyer.LocationId == model.LocationId);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Buyer already exists for this tracking user.");
        }

        if (!ModelState.IsValid)
        {
            model.BuyerOptions = await GetTrackingBuyerOptionsAsync(model.BuyerId);
            model.LocationOptions = model.BuyerId.HasValue ? await GetTrackingBuyerLocationOptionsAsync(model.BuyerId.Value, model.LocationId) : [];
            return View(model);
        }

        Context.UserBuyers.Add(new Data.Models.UserBuyer
        {
            UserId = model.CurrentUserId,
            BuyerId = model.BuyerId!.Value,
            LocationId = model.LocationId!.Value,
            OrderCount = 0,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Tracking), new { userId = model.CurrentUserId });
    }

    public async Task<IActionResult> AddTrackingProduct(int userBuyerId)
    {
        var model = await BuildTrackingProductFormAsync(userBuyerId, null);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrackingProduct(BuyerTrackingProductFormViewModel model)
    {
        var userBuyer = await Context.UserBuyers
            .AsNoTracking()
            .Where(userBuyer => userBuyer.Id == model.UserBuyerId)
            .Select(userBuyer => new { userBuyer.Id, userBuyer.UserId, userBuyer.BuyerId, userBuyer.LocationId })
            .FirstOrDefaultAsync();
        if (userBuyer is null)
        {
            return NotFound();
        }

        model.CurrentUserId = userBuyer.UserId;
        model.BuyerId = userBuyer.BuyerId;
        model.BuyerLocationId = userBuyer.LocationId;

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingProductFormAsync(model.UserBuyerId, model.ProductId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        var productIsValid = await BuyerTrackingProductExistsAsync(userBuyer.BuyerId, userBuyer.LocationId, model.ProductId!.Value);
        if (!productIsValid)
        {
            ModelState.AddModelError(nameof(model.ProductId), "Choose a product tied to this buyer location.");
        }

        var duplicate = await Context.BuyerTrackingProducts.AnyAsync(product =>
            product.UserBuyerId == model.UserBuyerId
            && product.ProductId == model.ProductId);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Product already exists for this tracking buyer.");
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingProductFormAsync(model.UserBuyerId, model.ProductId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        Context.BuyerTrackingProducts.Add(new Data.Models.BuyerTrackingProduct
        {
            UserBuyerId = model.UserBuyerId,
            ProductId = model.ProductId!.Value,
            Order = 0,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Tracking), new { userId = userBuyer.UserId });
    }

    public async Task<IActionResult> AddTrackingSupplier(int buyerTrackingProductId)
    {
        var model = await BuildTrackingSupplierFormAsync(buyerTrackingProductId, null, null);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrackingSupplier(BuyerTrackingSupplierFormViewModel model)
    {
        var trackingProduct = await Context.BuyerTrackingProducts
            .AsNoTracking()
            .Where(product => product.Id == model.BuyerTrackingProductId)
            .Select(product => new
            {
                product.Id,
                product.ProductId,
                product.UserBuyerId,
                product.UserBuyer.UserId,
                product.UserBuyer.BuyerId,
                product.UserBuyer.LocationId,
            })
            .FirstOrDefaultAsync();
        if (trackingProduct is null)
        {
            return NotFound();
        }

        model.CurrentUserId = trackingProduct.UserId;
        model.UserBuyerId = trackingProduct.UserBuyerId;
        model.BuyerId = trackingProduct.BuyerId;
        model.BuyerLocationId = trackingProduct.LocationId;
        model.ProductId = trackingProduct.ProductId;

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingSupplierFormAsync(model.BuyerTrackingProductId, model.SupplierId, model.SupplierLocationId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        var supplierIsValid = await BuyerTrackingSupplierLocationExistsAsync(
            trackingProduct.ProductId,
            trackingProduct.BuyerId,
            trackingProduct.LocationId,
            model.SupplierId!.Value,
            model.SupplierLocationId!.Value);
        if (!supplierIsValid)
        {
            ModelState.AddModelError(nameof(model.SupplierLocationId), "Choose an active supplier location tied to this buyer product.");
        }

        var duplicate = await Context.BuyerTrackingProductSuppliers.AnyAsync(supplier =>
            supplier.BuyerProductId == model.BuyerTrackingProductId
            && supplier.SupplierId == model.SupplierId
            && supplier.LocationId == model.SupplierLocationId);
        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Supplier already exists for this tracking product.");
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildTrackingSupplierFormAsync(model.BuyerTrackingProductId, model.SupplierId, model.SupplierLocationId);
            return rebuilt is null ? NotFound() : View(rebuilt);
        }

        Context.BuyerTrackingProductSuppliers.Add(new Data.Models.BuyerTrackingProductSupplier
        {
            UserBuyerId = trackingProduct.UserBuyerId,
            BuyerProductId = model.BuyerTrackingProductId,
            SupplierId = model.SupplierId!.Value,
            LocationId = model.SupplierLocationId!.Value,
            OrderCount = 0,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        });
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Tracking), new { userId = trackingProduct.UserId });
    }

    public async Task<IActionResult> GetTrackingBuyerLocations(int buyerId)
    {
        var locations = await GetTrackingBuyerLocationOptionsAsync(buyerId, null);
        return Json(locations.Select(location => new { id = location.Value, text = location.Text }));
    }

    public async Task<IActionResult> GetTrackingSupplierLocations(int buyerTrackingProductId, int supplierId)
    {
        var trackingProduct = await Context.BuyerTrackingProducts
            .AsNoTracking()
            .Where(product => product.Id == buyerTrackingProductId)
            .Select(product => new
            {
                product.ProductId,
                product.UserBuyer.BuyerId,
                product.UserBuyer.LocationId,
            })
            .FirstOrDefaultAsync();
        if (trackingProduct is null)
        {
            return NotFound();
        }

        var locations = await GetTrackingSupplierLocationOptionsAsync(
            trackingProduct.ProductId,
            trackingProduct.BuyerId,
            trackingProduct.LocationId,
            supplierId,
            null);

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

    private async Task<IEnumerable<SelectListItem>> GetTrackingBuyerOptionsAsync(int? selectedId = null) =>
        await Context.Buyers
            .AsNoTracking()
            .Where(buyer => buyer.IsActive == true)
            .OrderBy(buyer => buyer.Name)
            .Select(buyer => new SelectListItem
            {
                Value = buyer.Id.ToString(),
                Text = buyer.Name,
                Selected = selectedId == buyer.Id,
            })
            .ToListAsync();

    private async Task<List<SelectListItem>> GetTrackingBuyerLocationOptionsAsync(int buyerId, int? selectedId) =>
        await Context.Locations
            .AsNoTracking()
            .Where(location =>
                location.ClientId == buyerId
                && location.IsActive
                && location.IsBuyer == true)
            .OrderBy(location => location.Location1)
            .Select(location => new SelectListItem
            {
                Value = location.Id.ToString(),
                Text = location.Location1,
                Selected = selectedId == location.Id,
            })
            .ToListAsync();

    private async Task<BuyerTrackingProductFormViewModel?> BuildTrackingProductFormAsync(int userBuyerId, int? selectedProductId)
    {
        var userBuyer = await Context.UserBuyers
            .AsNoTracking()
            .Where(userBuyer => userBuyer.Id == userBuyerId)
            .Select(userBuyer => new
            {
                userBuyer.Id,
                userBuyer.UserId,
                userBuyer.BuyerId,
                userBuyer.LocationId,
                BuyerName = userBuyer.Buyer.Name,
                LocationName = userBuyer.Location.Location1,
            })
            .FirstOrDefaultAsync();
        if (userBuyer is null)
        {
            return null;
        }

        return new BuyerTrackingProductFormViewModel
        {
            CurrentUserId = userBuyer.UserId,
            UserBuyerId = userBuyer.Id,
            BuyerId = userBuyer.BuyerId,
            BuyerLocationId = userBuyer.LocationId,
            BuyerName = $"{userBuyer.BuyerName} - {userBuyer.LocationName}",
            ProductId = selectedProductId,
            ProductOptions = await GetTrackingProductOptionsAsync(userBuyer.BuyerId, userBuyer.LocationId, selectedProductId),
        };
    }

    private async Task<BuyerTrackingSupplierFormViewModel?> BuildTrackingSupplierFormAsync(int buyerTrackingProductId, int? selectedSupplierId, int? selectedLocationId)
    {
        var trackingProduct = await Context.BuyerTrackingProducts
            .AsNoTracking()
            .Where(product => product.Id == buyerTrackingProductId)
            .Select(product => new
            {
                product.Id,
                product.ProductId,
                product.UserBuyerId,
                product.UserBuyer.UserId,
                product.UserBuyer.BuyerId,
                product.UserBuyer.LocationId,
                ProductName = product.Product.Name,
            })
            .FirstOrDefaultAsync();
        if (trackingProduct is null)
        {
            return null;
        }

        return new BuyerTrackingSupplierFormViewModel
        {
            CurrentUserId = trackingProduct.UserId,
            UserBuyerId = trackingProduct.UserBuyerId,
            BuyerTrackingProductId = trackingProduct.Id,
            BuyerId = trackingProduct.BuyerId,
            BuyerLocationId = trackingProduct.LocationId,
            ProductId = trackingProduct.ProductId,
            ProductName = trackingProduct.ProductName ?? string.Empty,
            SupplierId = selectedSupplierId,
            SupplierLocationId = selectedLocationId,
            SupplierOptions = await GetTrackingSupplierOptionsAsync(trackingProduct.ProductId, trackingProduct.BuyerId, trackingProduct.LocationId, selectedSupplierId),
            LocationOptions = selectedSupplierId.HasValue
                ? await GetTrackingSupplierLocationOptionsAsync(trackingProduct.ProductId, trackingProduct.BuyerId, trackingProduct.LocationId, selectedSupplierId.Value, selectedLocationId)
                : [],
        };
    }

    private async Task<List<SelectListItem>> GetTrackingProductOptionsAsync(int buyerId, int buyerLocationId, int? selectedId) =>
        await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product =>
                product.BuyerSupplier != null
                && product.BuyerSupplier.Buyer == buyerId
                && product.BuyerSupplier.BuyerLocation == buyerLocationId
                && product.BuyerSupplier.IsActive == true
                && product.SupplierProductNavigation != null
                && product.SupplierProductNavigation.IsActive
                && product.SupplierProductNavigation.ProductNavigation != null
                && product.SupplierProductNavigation.ProductNavigation.IsActive == true
                && product.SupplierProductNavigation.SupplierNavigation != null
                && product.SupplierProductNavigation.SupplierNavigation.IsActive == true
                && product.SupplierProductNavigation.LocationNavigation != null
                && product.SupplierProductNavigation.LocationNavigation.IsActive)
            .Select(product => new
            {
                Id = product.SupplierProductNavigation!.Product!.Value,
                Name = product.SupplierProductNavigation.ProductNavigation!.Name ?? string.Empty,
            })
            .Distinct()
            .OrderBy(product => product.Name)
            .Select(product => new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = product.Name,
                Selected = selectedId == product.Id,
            })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetTrackingSupplierOptionsAsync(int productId, int buyerId, int buyerLocationId, int? selectedId) =>
        await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product =>
                product.BuyerSupplier != null
                && product.BuyerSupplier.Buyer == buyerId
                && product.BuyerSupplier.BuyerLocation == buyerLocationId
                && product.BuyerSupplier.IsActive == true
                && product.SupplierProductNavigation != null
                && product.SupplierProductNavigation.IsActive
                && product.SupplierProductNavigation.Product == productId
                && product.SupplierProductNavigation.SupplierNavigation != null
                && product.SupplierProductNavigation.SupplierNavigation.IsActive == true)
            .Select(product => new
            {
                Id = product.SupplierProductNavigation!.Supplier!.Value,
                Name = product.SupplierProductNavigation.SupplierNavigation!.Name ?? string.Empty,
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

    private async Task<List<SelectListItem>> GetTrackingSupplierLocationOptionsAsync(int productId, int buyerId, int buyerLocationId, int supplierId, int? selectedId) =>
        await Context.BuyerSupplierProducts
            .AsNoTracking()
            .Where(product =>
                product.BuyerSupplier != null
                && product.BuyerSupplier.Buyer == buyerId
                && product.BuyerSupplier.BuyerLocation == buyerLocationId
                && product.BuyerSupplier.IsActive == true
                && product.SupplierProductNavigation != null
                && product.SupplierProductNavigation.IsActive
                && product.SupplierProductNavigation.Product == productId
                && product.SupplierProductNavigation.Supplier == supplierId
                && product.SupplierProductNavigation.LocationNavigation != null
                && product.SupplierProductNavigation.LocationNavigation.IsActive
                && product.SupplierProductNavigation.LocationNavigation.IsBuyer == false)
            .Select(product => new
            {
                Id = product.SupplierProductNavigation!.Location!.Value,
                Name = product.SupplierProductNavigation.LocationNavigation!.Location1 ?? string.Empty,
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

    private async Task<bool> BuyerTrackingProductExistsAsync(int buyerId, int buyerLocationId, int productId) =>
        await Context.BuyerSupplierProducts.AnyAsync(product =>
            product.BuyerSupplier != null
            && product.BuyerSupplier.Buyer == buyerId
            && product.BuyerSupplier.BuyerLocation == buyerLocationId
            && product.BuyerSupplier.IsActive == true
            && product.SupplierProductNavigation != null
            && product.SupplierProductNavigation.IsActive
            && product.SupplierProductNavigation.Product == productId
            && product.SupplierProductNavigation.ProductNavigation != null
            && product.SupplierProductNavigation.ProductNavigation.IsActive == true
            && product.SupplierProductNavigation.SupplierNavigation != null
            && product.SupplierProductNavigation.SupplierNavigation.IsActive == true
            && product.SupplierProductNavigation.LocationNavigation != null
            && product.SupplierProductNavigation.LocationNavigation.IsActive
            && product.SupplierProductNavigation.LocationNavigation.IsBuyer == false);

    private async Task<bool> BuyerTrackingSupplierLocationExistsAsync(int productId, int buyerId, int buyerLocationId, int supplierId, int supplierLocationId) =>
        await Context.BuyerSupplierProducts.AnyAsync(product =>
            product.BuyerSupplier != null
            && product.BuyerSupplier.Buyer == buyerId
            && product.BuyerSupplier.BuyerLocation == buyerLocationId
            && product.BuyerSupplier.IsActive == true
            && product.SupplierProductNavigation != null
            && product.SupplierProductNavigation.IsActive
            && product.SupplierProductNavigation.Product == productId
            && product.SupplierProductNavigation.Supplier == supplierId
            && product.SupplierProductNavigation.Location == supplierLocationId
            && product.SupplierProductNavigation.ProductNavigation != null
            && product.SupplierProductNavigation.ProductNavigation.IsActive == true
            && product.SupplierProductNavigation.SupplierNavigation != null
            && product.SupplierProductNavigation.SupplierNavigation.IsActive == true
            && product.SupplierProductNavigation.LocationNavigation != null
            && product.SupplierProductNavigation.LocationNavigation.IsActive
            && product.SupplierProductNavigation.LocationNavigation.IsBuyer == false);

    private async Task<IReadOnlyList<RecentLoadListItemViewModel>> GetRecentLoadsAsync(int buyerId)
    {
        var loads = await Context.Loads
            .AsNoTracking()
            .Where(load => load.Buyer == buyerId && load.IsActive == true)
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
