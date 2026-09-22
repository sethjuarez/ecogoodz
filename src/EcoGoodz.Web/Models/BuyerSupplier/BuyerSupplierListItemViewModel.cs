namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierListItemViewModel
{
    public int Id { get; set; }
    public string? BuyerName { get; set; }
    public string? SupplierName { get; set; }
    public string? BuyerLocationName { get; set; }
    public string? SupplierLocationName { get; set; }
    public string? StatusName { get; set; }
    public bool IsActive { get; set; }
}
