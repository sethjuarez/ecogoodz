namespace EcoGoodz.Web.Models.Shared;

public class RecentLoadListItemViewModel
{
    public int Id { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? StatusName { get; set; }
    public int? BuyerId { get; set; }
    public string? BuyerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? BuyerLocationId { get; set; }
    public string? BuyerLocationName { get; set; }
    public int? SupplierLocationId { get; set; }
    public string? SupplierLocationName { get; set; }
    public string Products { get; set; } = string.Empty;
}
