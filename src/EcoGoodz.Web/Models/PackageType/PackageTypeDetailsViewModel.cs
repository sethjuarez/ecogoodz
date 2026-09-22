namespace EcoGoodz.Web.Models.PackageType;

public class PackageTypeDetailsViewModel
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int BuyerProductCount { get; set; }
    public int SupplierProductCount { get; set; }
}
