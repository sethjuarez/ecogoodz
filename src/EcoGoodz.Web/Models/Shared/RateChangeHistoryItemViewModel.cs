namespace EcoGoodz.Web.Models.Shared;

public class RateChangeHistoryItemViewModel
{
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public DateTime? OldEffectiveDate { get; set; }
    public DateTime? NewEffectiveDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
}
