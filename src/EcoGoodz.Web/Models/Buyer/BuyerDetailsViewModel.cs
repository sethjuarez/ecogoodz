using EcoGoodz.Web.Models.Shared;

namespace EcoGoodz.Web.Models.Buyer;

public class BuyerDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountManagerName { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ProductCount { get; set; }
    public int SupplierCount { get; set; }
    public int LoadCount { get; set; }
    public IReadOnlyList<RecentLoadListItemViewModel> RecentLoads { get; set; } = [];
}
