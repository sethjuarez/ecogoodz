namespace EcoGoodz.Web.Models.Product;

public class ProductDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int BuyerCount { get; set; }
    public int SupplierCount { get; set; }
}
