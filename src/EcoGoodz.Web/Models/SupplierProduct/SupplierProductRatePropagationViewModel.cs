using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductRatePropagationViewModel
{
    public int SupplierProductId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? SuggestedRate { get; set; }
    public DateTime? SuggestedEffectiveDate { get; set; }
    public List<SupplierProductRatePropagationRowViewModel> BuyerProducts { get; set; } = [];
}

public class SupplierProductRatePropagationRowViewModel
{
    public int BuyerSupplierProductId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerLocationName { get; set; }
    public decimal? CurrentRate { get; set; }
    public DateTime? CurrentEffectiveDate { get; set; }
    public bool IsSelected { get; set; }

    [DataType(DataType.Currency)]
    public decimal? UpdatedRate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? UpdatedEffectiveDate { get; set; }
}
