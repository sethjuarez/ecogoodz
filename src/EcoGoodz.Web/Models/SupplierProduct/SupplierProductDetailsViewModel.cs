using EcoGoodz.Web.Models.Shared;

namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductDetailsViewModel
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? PackagingName { get; set; }
    public decimal? PackagingPrice { get; set; }
    public int? Volume { get; set; }
    public string? FrequencyLabel { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public IReadOnlyList<SupplierProductRateListItemViewModel> Rates { get; set; } = [];
    public IReadOnlyList<RateChangeHistoryItemViewModel> RateHistory { get; set; } = [];
    public SupplierProductRateFormViewModel NewRate { get; set; } = new();
}
