using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Location;

public class LocationFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    public int? CopyLocationId { get; set; }

    public string? CopyLocationName { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Location name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Client type")]
    public string? ClientType { get; set; }

    [Display(Name = "Buyer")]
    public int? BuyerClientId { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierClientId { get; set; }

    public int? Country { get; set; }

    public int? State { get; set; }

    [StringLength(200)]
    public string? City { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "ZIP / postal code")]
    public string? PinCode { get; set; }

    [StringLength(200)]
    [Display(Name = "Dock hours")]
    public string? DockHours { get; set; }

    [Display(Name = "Payment terms")]
    public int? PaymentTerms { get; set; }

    [Display(Name = "Buyer status")]
    public int? BuyerStatus { get; set; }

    [Display(Name = "Supplier status")]
    public int? SupplierStatus { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> ClientTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> CountryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StateOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PaymentTermOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BuyerStatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierStatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ClientType == "Buyer" && BuyerClientId is null)
        {
            yield return new ValidationResult("Choose a buyer for buyer locations.", new[] { nameof(BuyerClientId) });
        }

        if (ClientType == "Supplier" && SupplierClientId is null)
        {
            yield return new ValidationResult("Choose a supplier for supplier locations.", new[] { nameof(SupplierClientId) });
        }
    }
}
