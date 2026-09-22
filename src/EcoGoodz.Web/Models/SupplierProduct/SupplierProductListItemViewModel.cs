namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductListItemViewModel
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? PackagingName { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateTime? CurrentEffectiveDate { get; set; }
    public bool IsActive { get; set; }
}
