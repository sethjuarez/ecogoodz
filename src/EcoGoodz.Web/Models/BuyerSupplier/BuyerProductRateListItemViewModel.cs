namespace EcoGoodz.Web.Models.BuyerSupplier;

public class BuyerProductRateListItemViewModel
{
    public int Id { get; set; }
    public decimal? Price { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UserName { get; set; }
    public bool IsActive { get; set; }
}
