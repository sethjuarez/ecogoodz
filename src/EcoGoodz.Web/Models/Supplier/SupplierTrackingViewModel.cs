using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Supplier;

public class SupplierTrackingViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> UserOptions { get; set; } = [];
    public List<SupplierTrackingProductViewModel> Products { get; set; } = [];
}

public class SupplierTrackingProductViewModel
{
    public int UserProductId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public List<SupplierTrackingSupplierViewModel> Suppliers { get; set; } = [];
}

public class SupplierTrackingSupplierViewModel
{
    public int UserSupplierId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public string? SupplierNote { get; set; }
}

public class SupplierTrackingProductFormViewModel
{
    public int CurrentUserId { get; set; }

    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    public IEnumerable<SelectListItem> ProductOptions { get; set; } = [];
}

public class SupplierTrackingSupplierFormViewModel
{
    public int CurrentUserId { get; set; }
    public int UserProductId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier is required")]
    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Location is required")]
    [Display(Name = "Location")]
    public int? LocationId { get; set; }

    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = [];
    public IEnumerable<SelectListItem> LocationOptions { get; set; } = [];
}
