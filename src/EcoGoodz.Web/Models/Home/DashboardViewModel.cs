namespace EcoGoodz.Web.Models.Home;

public class DashboardViewModel
{
    public int ActiveBuyerCount { get; set; }
    public int ActiveSupplierCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int ActiveLoadCount { get; set; }
    public int LoadsThisMonth { get; set; }
    public IReadOnlyList<RecentLoadItem> RecentLoads { get; set; } = Array.Empty<RecentLoadItem>();
}

public class RecentLoadItem
{
    public int Id { get; set; }
    public string? BuyerName { get; set; }
    public string? SupplierName { get; set; }
    public string? StatusName { get; set; }
    public DateTime? ShipmentDate { get; set; }
}
