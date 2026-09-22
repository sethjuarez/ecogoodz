using EcoGoodz.Data;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private const int ResultsPerType = 6;
    private readonly EcoGoodzDbContext _context;

    public SearchController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = q?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return View(new GlobalSearchViewModel());
        }

        var numericQuery = int.TryParse(query.TrimStart('#'), out var parsedQuery)
            ? parsedQuery
            : (int?)null;
        var results = new List<GlobalSearchResultViewModel>();

        results.AddRange(await _context.Buyers
            .AsNoTracking()
            .Where(buyer => buyer.Name != null && buyer.Name.Contains(query))
            .OrderBy(buyer => buyer.Name)
            .Take(ResultsPerType)
            .Select(buyer => new GlobalSearchResultViewModel
            {
                Category = "Buyer",
                Title = buyer.Name ?? string.Empty,
                Description = buyer.IsActive == true ? "Active buyer" : "Inactive buyer",
                Controller = "Buyer",
                Action = "Details",
                RouteId = buyer.Id,
            })
            .ToListAsync());

        results.AddRange(await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Name != null && supplier.Name.Contains(query))
            .OrderBy(supplier => supplier.Name)
            .Take(ResultsPerType)
            .Select(supplier => new GlobalSearchResultViewModel
            {
                Category = "Supplier",
                Title = supplier.Name ?? string.Empty,
                Description = supplier.IsActive == true ? "Active supplier" : "Inactive supplier",
                Controller = "Supplier",
                Action = "Details",
                RouteId = supplier.Id,
            })
            .ToListAsync());

        results.AddRange(await _context.Locations
            .AsNoTracking()
            .Where(location =>
                (location.Location1 != null && location.Location1.Contains(query))
                || (location.City != null && location.City.Contains(query))
                || (location.Address != null && location.Address.Contains(query)))
            .OrderBy(location => location.Location1)
            .Take(ResultsPerType)
            .Select(location => new GlobalSearchResultViewModel
            {
                Category = location.IsBuyer == true ? "Buyer location" : location.IsBuyer == false ? "Supplier location" : "Location",
                Title = location.Location1 ?? $"Location #{location.Id}",
                Description = location.City,
                Controller = "Location",
                Action = "Details",
                RouteId = location.Id,
            })
            .ToListAsync());

        results.AddRange(await _context.Products
            .AsNoTracking()
            .Where(product => product.Name != null && product.Name.Contains(query))
            .OrderBy(product => product.Name)
            .Take(ResultsPerType)
            .Select(product => new GlobalSearchResultViewModel
            {
                Category = "Product",
                Title = product.Name ?? string.Empty,
                Description = product.IsActive == true ? "Active product" : "Inactive product",
                Controller = "Product",
                Action = "Details",
                RouteId = product.Id,
            })
            .ToListAsync());

        results.AddRange(await _context.Loads
            .AsNoTracking()
            .Where(load =>
                (numericQuery.HasValue && load.Id == numericQuery.Value)
                || (load.BuyerNavigation != null && load.BuyerNavigation.Name != null && load.BuyerNavigation.Name.Contains(query))
                || (load.SupplierNavigation != null && load.SupplierNavigation.Name != null && load.SupplierNavigation.Name.Contains(query)))
            .OrderByDescending(load => load.ShipmentDate)
            .ThenByDescending(load => load.Id)
            .Take(ResultsPerType)
            .Select(load => new GlobalSearchResultViewModel
            {
                Category = "Load",
                Title = $"Load #{load.Id}",
                Description = (load.BuyerNavigation != null ? load.BuyerNavigation.Name : null)
                    + " → "
                    + (load.SupplierNavigation != null ? load.SupplierNavigation.Name : null),
                Controller = "Load",
                Action = "Details",
                RouteId = load.Id,
            })
            .ToListAsync());

        results.AddRange(await _context.BuyerSuppliers
            .AsNoTracking()
            .Where(match =>
                (match.BuyerNavigation != null && match.BuyerNavigation.Name != null && match.BuyerNavigation.Name.Contains(query))
                || (match.SupplierNavigation != null && match.SupplierNavigation.Name != null && match.SupplierNavigation.Name.Contains(query))
                || (match.BuyerLocationNavigation != null && match.BuyerLocationNavigation.Location1 != null && match.BuyerLocationNavigation.Location1.Contains(query))
                || (match.SupplierLocationNavigation != null && match.SupplierLocationNavigation.Location1 != null && match.SupplierLocationNavigation.Location1.Contains(query)))
            .OrderBy(match => match.Id)
            .Take(ResultsPerType)
            .Select(match => new GlobalSearchResultViewModel
            {
                Category = "Match",
                Title = (match.BuyerNavigation != null ? match.BuyerNavigation.Name : null)
                    + " → "
                    + (match.SupplierNavigation != null ? match.SupplierNavigation.Name : null),
                Description = (match.BuyerLocationNavigation != null ? match.BuyerLocationNavigation.Location1 : null)
                    + " / "
                    + (match.SupplierLocationNavigation != null ? match.SupplierLocationNavigation.Location1 : null),
                Controller = "BuyerSupplier",
                Action = "Details",
                RouteId = match.Id,
            })
            .ToListAsync());

        return View(new GlobalSearchViewModel
        {
            Query = query,
            Results = results,
        });
    }
}
