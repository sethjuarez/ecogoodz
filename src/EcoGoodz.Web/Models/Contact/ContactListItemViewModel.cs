namespace EcoGoodz.Web.Models.Contact;

public class ContactListItemViewModel
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public bool IsBuyer { get; set; }
    public int? LocationId { get; set; }
    public string? ClientName { get; set; }
    public string? LocationName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? OfficePhone { get; set; }
    public string? CellPhone { get; set; }
    public string? Address { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool IsDockContact { get; set; }
    public bool IsActive { get; set; }
}
