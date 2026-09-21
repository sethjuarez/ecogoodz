namespace EcoGoodz.Web.Models.Supplier;

public class SupplierListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountManagerName { get; set; }
    public bool IsActive { get; set; }
}
