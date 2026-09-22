using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.SupplierProduct;

public class SupplierProductRateFormViewModel
{
    public int SupplierProductId { get; set; }

    [Required]
    [DataType(DataType.Currency)]
    public decimal? Price { get; set; }

    [Required]
    [Display(Name = "Effective date")]
    [DataType(DataType.Date)]
    public DateTime? EffectiveDate { get; set; } = DateTime.Today;
}
