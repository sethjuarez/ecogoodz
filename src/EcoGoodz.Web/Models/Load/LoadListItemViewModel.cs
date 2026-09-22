namespace EcoGoodz.Web.Models.Load;

public class LoadListItemViewModel
{
    public int Id { get; set; }
    public int? BuyerId { get; set; }
    public int? SupplierId { get; set; }
    public string? BuyerName { get; set; }
    public string? SupplierName { get; set; }
    public string? StatusName { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public bool IsActive { get; set; }
}
