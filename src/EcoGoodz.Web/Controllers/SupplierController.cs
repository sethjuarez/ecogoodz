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
