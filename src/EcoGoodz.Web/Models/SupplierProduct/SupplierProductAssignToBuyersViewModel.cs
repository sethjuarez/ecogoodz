using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductAssignToBuyersViewModel
{
    public int ProductId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierLocationName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? ProductRate { get; set; }
    public DateTime? ProductEffectiveDate { get; set; }
    public List<SupplierProductAssignBuyerRowViewModel> TiedBuyers { get; set; } = [];
    public List<SupplierProductAssignBuyerRowViewModel> ActiveBuyers { get; set; } = [];
}

public class SupplierProductAssignBuyerRowViewModel
{
    public int? BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public List<int> SelectedLocationIds { get; set; } = [];
    public List<SupplierProductAssignLocationViewModel> Locations { get; set; } = [];

    [DataType(DataType.Currency)]
    public decimal? Rate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EffectiveDate { get; set; }
}

public class SupplierProductAssignLocationViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
