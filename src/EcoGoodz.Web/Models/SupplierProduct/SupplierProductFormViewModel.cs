using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Supplier")]
    public int? Supplier { get; set; }

    [Display(Name = "Location")]
    public int? Location { get; set; }

    [Display(Name = "Product")]
    public int? Product { get; set; }

    [Display(Name = "Packaging")]
    public int? Packaging { get; set; }

    [Display(Name = "Other packaging")]
    [StringLength(250)]
    public string? OtherPackaging { get; set; }

    [Display(Name = "Packaging price")]
    [DataType(DataType.Currency)]
    public decimal? PackagingPrice { get; set; }

    public int? Volume { get; set; }

    [Display(Name = "Frequency type")]
    public int? FrequencyPackageType { get; set; }

    public int? Frequency { get; set; }

    [Display(Name = "Initial product price")]
    [DataType(DataType.Currency)]
    public decimal? InitialPrice { get; set; }

    [Display(Name = "Initial effective date")]
    [DataType(DataType.Date)]
    public DateTime? InitialEffectiveDate { get; set; } = DateTime.Today;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> LocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ProductOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PackagingOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> FrequencyPackageTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
