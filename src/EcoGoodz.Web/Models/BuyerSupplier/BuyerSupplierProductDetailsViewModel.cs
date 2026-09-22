using EcoGoodz.Web.Models.Shared;

namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierProductDetailsViewModel
{
    public int Id { get; set; }
    public int BuyerSupplierId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? PackagingName { get; set; }
    public decimal? SupplierPrice { get; set; }
    public DateTime? SupplierEffectiveDate { get; set; }
    public IReadOnlyList<BuyerProductRateListItemViewModel> Rates { get; set; } = [];
    public IReadOnlyList<RateChangeHistoryItemViewModel> RateHistory { get; set; } = [];
    public BuyerProductRateFormViewModel NewRate { get; set; } = new();
}
