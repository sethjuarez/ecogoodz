namespace EcoGoodz.Web.Models.BuyerProduct;

public class BuyerProductListItemViewModel
{
    public int Id { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? PackagingName { get; set; }
    public bool IsActive { get; set; }
}
