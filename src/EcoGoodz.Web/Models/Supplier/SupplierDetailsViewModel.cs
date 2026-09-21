namespace EcoGoodz.Web.Models.Supplier;

public class SupplierDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ProductCount { get; set; }
    public int BuyerCount { get; set; }
    public int LoadCount { get; set; }
}
