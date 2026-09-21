namespace EcoGoodz.Web.Models.Product;

public class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public bool IsActive { get; set; }
}
