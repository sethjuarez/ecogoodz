using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.PackageType;

public class PackageTypeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Package type")]
    public string Type { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
