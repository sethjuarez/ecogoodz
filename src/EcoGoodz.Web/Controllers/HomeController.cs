using System.Diagnostics;
using EcoGoodz.Data;
using EcoGoodz.Web.Models;
using EcoGoodz.Web.Models.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly EcoGoodzDbContext _context;

    public HomeController(ILogger<HomeController> logger, EcoGoodzDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var model = new DashboardViewModel
        {
            ActiveBuyerCount = await _context.Buyers.CountAsync(b => b.IsActive == true),
            ActiveSupplierCount = await _context.Suppliers.CountAsync(s => s.IsActive == true),
            ActiveProductCount = await _context.Products.CountAsync(p => p.IsActive == true),
            ActiveLoadCount = await _context.Loads.CountAsync(l => l.IsActive == true),
            LoadsThisMonth = await _context.Loads.CountAsync(l => l.ShipmentDate >= monthStart),
            RecentLoads = await _context.Loads
                .Include(l => l.BuyerNavigation)
                .Include(l => l.SupplierNavigation)
                .Include(l => l.LoadStatusNavigation)
                .OrderByDescending(l => l.CreateOn)
                .Take(8)
                .Select(l => new RecentLoadItem
                {
                    Id = l.Id,
                    BuyerId = l.Buyer,
                    BuyerName = l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
                    SupplierId = l.Supplier,
                    SupplierName = l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
                    StatusName = l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
                    ShipmentDate = l.ShipmentDate,
                })
                .ToListAsync(),
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
