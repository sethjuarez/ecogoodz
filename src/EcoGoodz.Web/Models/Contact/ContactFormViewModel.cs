using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Contact;

public class ContactFormViewModel
{
    public int Id { get; set; }

    public int? ContactInformationId { get; set; }

    [Required]
    [Display(Name = "Location")]
    public int? LocationId { get; set; }

    public int? ClientId { get; set; }
    public bool? IsBuyer { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Office phone")]
    public string? OfficePhone { get; set; }

    [StringLength(100)]
    [Display(Name = "Cell phone")]
    public string? CellPhone { get; set; }

    [StringLength(256)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "State")]
    public int? State { get; set; }

    [Display(Name = "Country")]
    public int? Country { get; set; }

    [StringLength(50)]
    [Display(Name = "ZIP / postal code")]
    public string? PinCode { get; set; }

    [Display(Name = "Primary contact")]
    public bool IsPrimaryContact { get; set; }

    [Display(Name = "Dock contact")]
    public bool IsDockContact { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> LocationOptions { get; set; } = [];
    public IEnumerable<SelectListItem> StateOptions { get; set; } = [];
    public IEnumerable<SelectListItem> CountryOptions { get; set; } = [];
}
