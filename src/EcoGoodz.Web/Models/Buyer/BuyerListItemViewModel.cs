namespace EcoGoodz.Web.Models.Buyer;

public class BuyerListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AccountManagerId { get; set; }
    public string? AccountManagerName { get; set; }
    public bool IsActive { get; set; }
}
