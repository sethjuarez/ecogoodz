namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierDetailsViewModel
{
    public int Id { get; set; }
    public int? BuyerId { get; set; }
    public int? SupplierId { get; set; }
    public int? BuyerLocationId { get; set; }
    public int? SupplierLocationId { get; set; }
    public string? BuyerName { get; set; }
    public string? SupplierName { get; set; }
    public string? BuyerLocationName { get; set; }
    public string? SupplierLocationName { get; set; }
    public string? StatusName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ProductCount { get; set; }
    public int LoadCount { get; set; }
    public IReadOnlyList<BuyerSupplierProductListItemViewModel> Products { get; set; } = [];
    public BuyerSupplierProductFormViewModel NewProduct { get; set; } = new();
}
