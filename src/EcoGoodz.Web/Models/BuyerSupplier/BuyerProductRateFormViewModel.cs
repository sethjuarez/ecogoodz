using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerProductRateFormViewModel
{
    public int BuyerSupplierId { get; set; }
    public int BuyerSupplierProductId { get; set; }

    [Required]
    [DataType(DataType.Currency)]
    public decimal? Price { get; set; }

    [Required]
    [Display(Name = "Effective date")]
    [DataType(DataType.Date)]
    public DateTime? EffectiveDate { get; set; } = DateTime.Today;
}
