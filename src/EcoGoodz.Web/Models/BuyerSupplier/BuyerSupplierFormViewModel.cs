using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Buyer")]
    public int? Buyer { get; set; }

    [Display(Name = "Supplier")]
    public int? Supplier { get; set; }

    [Display(Name = "Buyer location")]
    public int? BuyerLocation { get; set; }

    [Display(Name = "Supplier location")]
    public int? SupplierLocation { get; set; }

    public int? Status { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BuyerLocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierLocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
