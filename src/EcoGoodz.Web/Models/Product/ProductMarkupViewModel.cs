using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Product;

public class ProductMarkupViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public List<ProductMarkupColorLineViewModel> MarkupColors { get; set; } = [];
}

public class ProductMarkupColorLineViewModel
{
    public int? Id { get; set; }
    public int MarkUpColorId { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? ColorCode { get; set; }

    [Display(Name = "Minimum margin")]
    public decimal? MinRate { get; set; }

    [Display(Name = "Maximum margin")]
    public decimal? MaxRate { get; set; }
}
