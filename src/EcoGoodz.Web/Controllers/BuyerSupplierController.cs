using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.BuyerSupplier;
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
            BuyerName = m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
            SupplierName = m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
            BuyerLocationName = m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
            SupplierLocationName = m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
            StatusName = m.StatusNavigation != null ? m.StatusNavigation.Status : null,
            IsActive = m.IsActive ?? false,
        };

    public async Task<IActionResult> Details(int id)
    {
        var match = await Context.BuyerSuppliers
            .Where(m => m.Id == id)
            .Select(m => new BuyerSupplierDetailsViewModel
            {
                Id = m.Id,
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
