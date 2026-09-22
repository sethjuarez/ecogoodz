using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private static readonly int[] LegacyLastLoadStatusIds = [2, 4];

    private readonly EcoGoodzDbContext _context;

    public ReportController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> LastLoadShipped(LastLoadShippedReportViewModel filters)
    {
        filters.ClientType = string.Equals(filters.ClientType, "Supplier", StringComparison.OrdinalIgnoreCase)
            ? "Supplier"
            : "Buyer";

        filters.StartDate ??= DateTime.Today.AddMonths(-13);
        filters.EndDate ??= DateTime.Today;

        filters.AccountManagers = await GetAccountManagersAsync(filters.IsBuyerReport);

        if (!ModelState.IsValid)
        {
            filters.Rows = [];
            return View(filters);
        }

        filters.Rows = await GetLastLoadRowsAsync(filters);
        return View(filters);
    }

    public IActionResult Index() => RedirectToAction(nameof(LastLoadShipped));

    private async Task<List<AccountManagerReportOption>> GetAccountManagersAsync(bool isBuyerReport)
    {
        var managerIds = isBuyerReport
            ? await BaseReportLoads()
                .Select(load => load.BuyerAccountMgr ?? load.BuyerNavigation!.AccountManager)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync()
            : await BaseReportLoads()
                .Select(load => load.SupplierAccountMgr ?? load.SupplierNavigation!.AccountManager)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync();

        var users = await _context.Users
            .AsNoTracking()
            .Where(user => managerIds.Contains(user.Id))
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync();

        return users
            .Select(user => new AccountManagerReportOption
            {
                Id = user.Id,
                Name = FormatUserName(user),
            })
            .ToList();
    }

    private async Task<List<LastLoadShippedReportRow>> GetLastLoadRowsAsync(LastLoadShippedReportViewModel filters)
    {
        var query = BaseReportLoads();

        if (filters.StaleDays is null or <= 0)
        {
            query = query.Where(load =>
                (!filters.StartDate.HasValue || load.ShipmentDate!.Value.Date >= filters.StartDate.Value.Date)
                && (!filters.EndDate.HasValue || load.ShipmentDate!.Value.Date <= filters.EndDate.Value.Date));
        }

        if (filters.AccountManagerId.HasValue)
        {
            query = filters.IsBuyerReport
                ? query.Where(load => (load.BuyerAccountMgr ?? load.BuyerNavigation!.AccountManager) == filters.AccountManagerId.Value)
                : query.Where(load => (load.SupplierAccountMgr ?? load.SupplierNavigation!.AccountManager) == filters.AccountManagerId.Value);
        }

        var latestLoadIds = filters.IsBuyerReport
            ? await query
                .GroupBy(load => load.Buyer)
                .Select(group => group
                    .OrderByDescending(load => load.ShipmentDate)
                    .ThenByDescending(load => load.Id)
                    .Select(load => load.Id)
                    .First())
                .ToListAsync()
            : await query
                .GroupBy(load => load.Supplier)
                .Select(group => group
                    .OrderByDescending(load => load.ShipmentDate)
                    .ThenByDescending(load => load.Id)
                    .Select(load => load.Id)
                    .First())
                .ToListAsync();

        var latestLoadsQuery = _context.Loads
            .AsNoTracking()
            .Where(load => latestLoadIds.Contains(load.Id));

        if (filters.StaleDays is > 0)
        {
            var staleCutoff = DateTime.Today.AddDays(-filters.StaleDays.Value);
            latestLoadsQuery = latestLoadsQuery.Where(load => load.ShipmentDate <= staleCutoff);
        }

        var latestLoads = await latestLoadsQuery
            .Include(load => load.BuyerNavigation)
                .ThenInclude(buyer => buyer!.AccountManagerNavigation)
            .Include(load => load.SupplierNavigation)
                .ThenInclude(supplier => supplier!.AccountManagerNavigation)
            .Include(load => load.BuyerAccountMgrNavigation)
            .Include(load => load.SupplierAccountMgrNavigation)
            .Include(load => load.BuyerLocationNavigation)
            .Include(load => load.SupplierLocationNavigation)
            .Include(load => load.LoadProducts)
                .ThenInclude(loadProduct => loadProduct.ProductNavigation)
                    .ThenInclude(supplierProduct => supplierProduct.ProductNavigation)
            .AsSplitQuery()
            .ToListAsync();

        var communications = await _context.Communications
            .AsNoTracking()
            .Where(communication => communication.IsActive && communication.ClientId.HasValue && communication.IsBuyer == filters.IsBuyerReport)
            .GroupBy(communication => communication.ClientId!.Value)
            .Select(group => new
            {
                ClientId = group.Key,
                LastDate = group.Max(communication => communication.Date),
            })
            .ToDictionaryAsync(item => item.ClientId, item => item.LastDate);

        return latestLoads
            .Select(load => ToReportRow(load, filters.IsBuyerReport, communications))
            .OrderByDescending(row => row.DaysSinceShipment)
            .ThenBy(row => row.ClientName)
            .ToList();
    }

    private IQueryable<Load> BaseReportLoads() =>
        _context.Loads
            .AsNoTracking()
            .Where(load =>
                load.IsActive == true
                && load.ShipmentDate.HasValue
                && load.LoadStatus.HasValue
                && LegacyLastLoadStatusIds.Contains(load.LoadStatus.Value)
                && load.Buyer.HasValue
                && load.Supplier.HasValue);

    private static LastLoadShippedReportRow ToReportRow(
        Load load,
        bool isBuyerReport,
        IReadOnlyDictionary<int, DateTime?> lastCommunicationDates)
    {
        var clientId = isBuyerReport ? load.Buyer : load.Supplier;
        var clientName = isBuyerReport
            ? load.BuyerNavigation?.Name
            : load.SupplierNavigation?.Name;
        var accountManager = isBuyerReport
            ? load.BuyerAccountMgrNavigation ?? load.BuyerNavigation?.AccountManagerNavigation
            : load.SupplierAccountMgrNavigation ?? load.SupplierNavigation?.AccountManagerNavigation;
        var locationName = isBuyerReport
            ? load.BuyerLocationNavigation?.Location1
            : load.SupplierLocationNavigation?.Location1;
        var products = load.LoadProducts
            .Select(loadProduct => loadProduct.ProductNavigation?.ProductNavigation?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return new LastLoadShippedReportRow
        {
            ClientId = clientId,
            ClientName = clientName ?? "(unknown)",
            AccountManagerName = accountManager is null ? null : FormatUserName(accountManager),
            LocationName = locationName,
            ShipmentDate = load.ShipmentDate!.Value.Date,
            DaysSinceShipment = (int)(DateTime.Today - load.ShipmentDate.Value.Date).TotalDays,
            Products = string.Join(", ", products),
            LastCommunicationDate = clientId.HasValue && lastCommunicationDates.TryGetValue(clientId.Value, out var lastDate)
                ? lastDate
                : null,
        };
    }

    private static string FormatUserName(User user)
    {
        var name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(name) ? user.UserName ?? user.Email ?? $"User {user.Id}" : name;
    }
}
