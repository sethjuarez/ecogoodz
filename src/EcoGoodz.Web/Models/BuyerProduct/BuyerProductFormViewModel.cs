using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.BuyerProduct;

public class BuyerProductFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Buyer")]
    public int? Buyer { get; set; }

    [Display(Name = "Location")]
    public int? Location { get; set; }

    [Display(Name = "Product")]
    public int? Product { get; set; }

    [Display(Name = "Other product")]
    [StringLength(250)]
    public string? OtherProduct { get; set; }

    [Display(Name = "Packaging")]
    public int? Packaging { get; set; }

    [Display(Name = "Other packaging")]
    [StringLength(250)]
    public string? OtherPackaging { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> LocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ProductOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PackagingOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
