using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.StaffUser;

public class StaffUserFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? Initials { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Office phone")]
    public string? OfficePhone { get; set; }

    [StringLength(100)]
    [Display(Name = "Cell phone")]
    public string? CellPhone { get; set; }

    [Display(Name = "Role")]
    public int? Role { get; set; }

    [Display(Name = "Send company report")]
    public bool IsSendCompanyReport { get; set; }

    [Display(Name = "Send manager report")]
    public bool IsSendManagerReport { get; set; }

    [Display(Name = "Show communication report")]
    public bool IsShowCommunicationReport { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> RoleOptions { get; set; } = [];
}
