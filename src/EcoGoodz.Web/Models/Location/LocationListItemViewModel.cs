namespace EcoGoodz.Web.Models.Location;

public class LocationListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? CountryName { get; set; }
    public bool IsActive { get; set; }
}
