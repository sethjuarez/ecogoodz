using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerSupplierProductFormViewModel
{
    public int BuyerSupplierId { get; set; }

    [Required]
    [Display(Name = "Supplier product")]
    public int? SupplierProduct { get; set; }

    [Required]
    [Display(Name = "Buyer price")]
    [DataType(DataType.Currency)]
    public decimal? Price { get; set; }

    [Required]
    [Display(Name = "Effective date")]
    [DataType(DataType.Date)]
    public DateTime? EffectiveDate { get; set; } = DateTime.Today;

    public IEnumerable<SelectListItem> SupplierProductOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
