namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierProductListItemViewModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? PackagingName { get; set; }
    public decimal? BuyerPrice { get; set; }
    public DateTime? BuyerEffectiveDate { get; set; }
    public decimal? SupplierPrice { get; set; }
    public DateTime? SupplierEffectiveDate { get; set; }
}
