using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Product;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Parent category")]
    public int? IdParent { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> ParentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
