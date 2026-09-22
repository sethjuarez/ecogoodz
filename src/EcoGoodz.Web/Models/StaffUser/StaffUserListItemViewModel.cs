namespace EcoGoodz.Web.Models.StaffUser;

public class StaffUserListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? OfficePhone { get; set; }
    public string? CellPhone { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
}
