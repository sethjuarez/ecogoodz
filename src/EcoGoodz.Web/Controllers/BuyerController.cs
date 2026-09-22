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
