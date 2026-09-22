namespace EcoGoodz.Web.Models.Location;

public class LocationDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientType { get; set; }
    public string? ClientName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? CountryName { get; set; }
    public string? PinCode { get; set; }
    public string? DockHours { get; set; }
    public string? PaymentTermsName { get; set; }
    public string? BuyerStatusName { get; set; }
    public string? SupplierStatusName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int BuyerProductCount { get; set; }
    public int SupplierProductCount { get; set; }
    public int LoadCount { get; set; }
    public int MatchCount { get; set; }
}
