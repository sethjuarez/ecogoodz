using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Note;

public class NoteFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Scope")]
    public string Scope { get; set; } = "BuyerLocation";

    [Display(Name = "Buyer location")]
    public int? BuyerLocation { get; set; }

    [Display(Name = "Supplier location")]
    public int? SupplierLocation { get; set; }

    [Display(Name = "Supplier product")]
    public int? Product { get; set; }

    [Required]
    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> ScopeOptions { get; set; } = [];
    public IEnumerable<SelectListItem> BuyerLocationOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SupplierLocationOptions { get; set; } = [];
    public IEnumerable<SelectListItem> ProductOptions { get; set; } = [];
}
