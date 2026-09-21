using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Supplier;

public class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Account manager")]
    public int? AccountManager { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> AccountManagerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
