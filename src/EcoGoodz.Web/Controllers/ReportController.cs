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

    public async Task<IActionResult> Communication(CommunicationReportViewModel filters)
    {
        filters.StartDate ??= DateTime.Today.AddDays(-30);
        filters.EndDate ??= DateTime.Today;

        if (filters.StartDate.HasValue && filters.EndDate.HasValue && filters.StartDate.Value.Date > filters.EndDate.Value.Date)
        {
            ModelState.AddModelError(nameof(filters.EndDate), "End date must be on or after start date.");
        }

        filters.Users = await GetCommunicationReportUsersAsync();

        if (!ModelState.IsValid)
        {
            filters.Rows = [];
            return View(filters);
        }

        filters.Rows = await GetCommunicationReportRowsAsync(filters);
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

    private async Task<List<CommunicationReportUserColumn>> GetCommunicationReportUsersAsync()
    {
        var communicationUserIds = await _context.Communications
            .AsNoTracking()
            .Where(communication => communication.IsActive && communication.CreatedBy.HasValue)
            .Select(communication => communication.CreatedBy!.Value)
            .Distinct()
            .ToListAsync();

        var buyerSupplierHistoryUserIds = await _context.BuyerSupplierHistories
            .AsNoTracking()
            .Where(history => history.UserId.HasValue)
            .Select(history => history.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var activityUserIds = communicationUserIds
            .Union(buyerSupplierHistoryUserIds)
            .ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive == true && user.IsShowCommunicationReport && activityUserIds.Contains(user.Id))
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync();

        return users
            .Select(user => new CommunicationReportUserColumn
            {
                Id = user.Id,
                Name = FormatUserName(user),
            })
            .ToList();
    }

    private async Task<List<CommunicationReportRow>> GetCommunicationReportRowsAsync(CommunicationReportViewModel filters)
    {
        if (filters.Users.Count == 0)
        {
            return [];
        }

        var userIds = filters.Users.Select(user => user.Id).ToHashSet();
        var startDate = filters.StartDate?.Date;
        var endDate = filters.EndDate?.Date;
        var communicationTypes = await _context.CommunicationTypes
            .AsNoTracking()
            .OrderBy(type => type.Type)
            .Select(type => new CommunicationTypeReportOption(type.Id, type.Type ?? $"Type {type.Id}"))
            .ToListAsync();

        var communications = await _context.Communications
            .AsNoTracking()
            .Where(communication =>
                communication.IsActive
                && (communication.Date.HasValue || communication.CreateOn.HasValue)
                && communication.CreatedBy.HasValue
                && userIds.Contains(communication.CreatedBy.Value)
                && (!startDate.HasValue || (communication.Date ?? communication.CreateOn)!.Value.Date >= startDate.Value)
                && (!endDate.HasValue || (communication.Date ?? communication.CreateOn)!.Value.Date <= endDate.Value))
            .ToListAsync();

        var activeProductHistories = await _context.BuyerSupplierHistories
            .AsNoTracking()
            .Where(history =>
                history.UserId.HasValue
                && userIds.Contains(history.UserId.Value)
                && history.CreatedDate.HasValue
                && history.NewStatus == 1
                && (history.Action == null || history.Action.ToLower() != "add")
                && (!startDate.HasValue || history.CreatedDate.Value.Date >= startDate.Value)
                && (!endDate.HasValue || history.CreatedDate.Value.Date <= endDate.Value))
            .ToListAsync();

        var proposedProductHistories = await _context.BuyerSupplierHistories
            .AsNoTracking()
            .Where(history =>
                history.UserId.HasValue
                && userIds.Contains(history.UserId.Value)
                && history.CreatedDate.HasValue
                && history.NewStatus == 3
                && (!startDate.HasValue || history.CreatedDate.Value.Date >= startDate.Value)
                && (!endDate.HasValue || history.CreatedDate.Value.Date <= endDate.Value))
            .ToListAsync();

        var communicationsByDateUser = communications
            .GroupBy(communication => (Date: (communication.Date ?? communication.CreateOn)!.Value.Date, UserId: communication.CreatedBy!.Value))
            .ToDictionary(group => group.Key, group => group.ToList());
        var activeProductsByDateUser = activeProductHistories
            .GroupBy(history => (Date: history.CreatedDate!.Value.Date, UserId: history.UserId!.Value))
            .ToDictionary(group => group.Key, group => group.Count());
        var proposedProductsByDateUser = proposedProductHistories
            .GroupBy(history => (Date: history.CreatedDate!.Value.Date, UserId: history.UserId!.Value))
            .ToDictionary(group => group.Key, group => group.Count());

        var dates = communicationsByDateUser.Keys
            .Select(key => key.Date)
            .Union(activeProductHistories.Select(history => history.CreatedDate!.Value.Date))
            .Union(proposedProductHistories.Select(history => history.CreatedDate!.Value.Date))
            .OrderByDescending(date => date)
            .ToList();

        return dates
            .Select(date => new CommunicationReportRow
            {
                Date = date,
                UserCells = filters.Users
                    .Select(user => BuildCommunicationCell(
                        user.Id,
                        communicationTypes,
                        communicationsByDateUser.GetValueOrDefault((date, user.Id)) ?? [],
                        activeProductsByDateUser.GetValueOrDefault((date, user.Id)),
                        proposedProductsByDateUser.GetValueOrDefault((date, user.Id))))
                    .ToList(),
            })
            .ToList();
    }

    private static CommunicationReportUserCell BuildCommunicationCell(
        int userId,
        IEnumerable<CommunicationTypeReportOption> communicationTypes,
        IEnumerable<Communication> communications,
        int activeProductCount,
        int proposedProductCount)
    {
        var communicationList = communications.ToList();
        var counts = communicationTypes
            .Select(type =>
            {
                return new CommunicationReportCount
                {
                    Label = type.Name,
                    BuyerCount = communicationList.Count(communication => communication.CommunicationType == type.Id && communication.IsBuyer == true),
                    SupplierCount = communicationList.Count(communication => communication.CommunicationType == type.Id && communication.IsBuyer == false),
                };
            })
            .ToList();

        var otherBuyerCount = communicationList.Count(communication => !communication.CommunicationType.HasValue && communication.IsBuyer == true);
        var otherSupplierCount = communicationList.Count(communication => !communication.CommunicationType.HasValue && communication.IsBuyer == false);
        if (otherBuyerCount > 0 || otherSupplierCount > 0)
        {
            counts.Add(new CommunicationReportCount
            {
                Label = "Other Type",
                BuyerCount = otherBuyerCount,
                SupplierCount = otherSupplierCount,
            });
        }

        counts.Add(new CommunicationReportCount { Label = "Active Products", BuyerCount = activeProductCount, IsProductMetric = true });
        counts.Add(new CommunicationReportCount { Label = "Proposed Products", BuyerCount = proposedProductCount, IsProductMetric = true });

        return new CommunicationReportUserCell
        {
            UserId = userId,
            Counts = counts,
        };
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
            .Select(group => group
                .OrderByDescending(communication => communication.Date ?? communication.CreateOn)
                .ThenByDescending(communication => communication.Id)
                .Select(communication => new
                {
                    ClientId = communication.ClientId!.Value,
                    communication.Id,
                    Date = communication.Date ?? communication.CreateOn,
                })
                .First())
            .ToDictionaryAsync(
                item => item.ClientId,
                item => new LatestCommunicationReportTarget(item.Id, item.Date));

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

    private sealed record CommunicationTypeReportOption(int Id, string Name);

    private sealed record LatestCommunicationReportTarget(int Id, DateTime? Date);

    private static LastLoadShippedReportRow ToReportRow(
        Load load,
        bool isBuyerReport,
        IReadOnlyDictionary<int, LatestCommunicationReportTarget> lastCommunicationDates)
    {
        var clientId = isBuyerReport ? load.Buyer : load.Supplier;
        var clientName = isBuyerReport
            ? load.BuyerNavigation?.Name
            : load.SupplierNavigation?.Name;
        var accountManager = isBuyerReport
            ? load.BuyerAccountMgrNavigation ?? load.BuyerNavigation?.AccountManagerNavigation
            : load.SupplierAccountMgrNavigation ?? load.SupplierNavigation?.AccountManagerNavigation;
        var accountManagerId = isBuyerReport
            ? load.BuyerAccountMgr ?? load.BuyerNavigation?.AccountManager
            : load.SupplierAccountMgr ?? load.SupplierNavigation?.AccountManager;
        var locationId = isBuyerReport ? load.BuyerLocation : load.SupplierLocation;
        var locationName = isBuyerReport
            ? load.BuyerLocationNavigation?.Location1
            : load.SupplierLocationNavigation?.Location1;
        var products = load.LoadProducts
            .Select(loadProduct => loadProduct.ProductNavigation?.ProductNavigation?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return new LastLoadShippedReportRow
        {
            LoadId = load.Id,
            ClientId = clientId,
            ClientName = clientName ?? "(unknown)",
            AccountManagerId = accountManagerId,
            AccountManagerName = accountManager is null ? null : FormatUserName(accountManager),
            LocationId = locationId,
            LocationName = locationName,
            ShipmentDate = load.ShipmentDate!.Value.Date,
            DaysSinceShipment = (int)(DateTime.Today - load.ShipmentDate.Value.Date).TotalDays,
            Products = string.Join(", ", products),
            LastCommunicationId = clientId.HasValue && lastCommunicationDates.TryGetValue(clientId.Value, out var lastCommunication)
                ? lastCommunication.Id
                : null,
            LastCommunicationDate = clientId.HasValue && lastCommunicationDates.TryGetValue(clientId.Value, out lastCommunication)
                ? lastCommunication.Date
                : null,
        };
    }

    private static string FormatUserName(User user)
    {
        var name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(name) ? user.UserName ?? user.Email ?? $"User {user.Id}" : name;
    }
}
