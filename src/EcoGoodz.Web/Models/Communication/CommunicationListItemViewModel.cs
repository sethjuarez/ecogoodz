namespace EcoGoodz.Web.Models.Communication;

public class CommunicationListItemViewModel
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public bool IsBuyer { get; set; }
    public string? ClientName { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? Date { get; set; }
    public string? CreatedByName { get; set; }
}
