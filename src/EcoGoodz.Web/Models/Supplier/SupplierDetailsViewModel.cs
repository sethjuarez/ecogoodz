using EcoGoodz.Web.Models.Shared;

namespace EcoGoodz.Web.Models.Supplier;

public class SupplierDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AccountManagerId { get; set; }
    public string? AccountManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ProductCount { get; set; }
    public int BuyerCount { get; set; }
    public int LoadCount { get; set; }
    public IReadOnlyList<RecentLoadListItemViewModel> RecentLoads { get; set; } = [];
}
