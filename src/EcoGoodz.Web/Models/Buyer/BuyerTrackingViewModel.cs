using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Buyer;

public class BuyerTrackingViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> UserOptions { get; set; } = [];
    public List<BuyerTrackingBuyerViewModel> Buyers { get; set; } = [];
}

public class BuyerTrackingBuyerViewModel
{
    public int UserBuyerId { get; set; }
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public List<BuyerTrackingProductViewModel> Products { get; set; } = [];
}

public class BuyerTrackingProductViewModel
{
    public int BuyerTrackingProductId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public List<BuyerTrackingSupplierViewModel> Suppliers { get; set; } = [];
}

public class BuyerTrackingSupplierViewModel
{
    public int BuyerTrackingProductSupplierId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public string? SupplierNote { get; set; }
}

public class BuyerTrackingBuyerFormViewModel
{
    public int CurrentUserId { get; set; }

    [Required(ErrorMessage = "Buyer is required")]
    [Display(Name = "Buyer")]
    public int? BuyerId { get; set; }

    [Required(ErrorMessage = "Location is required")]
    [Display(Name = "Location")]
    public int? LocationId { get; set; }

    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = [];
    public IEnumerable<SelectListItem> LocationOptions { get; set; } = [];
}

public class BuyerTrackingProductFormViewModel
{
    public int CurrentUserId { get; set; }
    public int UserBuyerId { get; set; }
    public int BuyerId { get; set; }
    public int BuyerLocationId { get; set; }
    public string BuyerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    public IEnumerable<SelectListItem> ProductOptions { get; set; } = [];
}

public class BuyerTrackingSupplierFormViewModel
{
    public int CurrentUserId { get; set; }
    public int UserBuyerId { get; set; }
    public int BuyerTrackingProductId { get; set; }
    public int BuyerId { get; set; }
    public int BuyerLocationId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier is required")]
    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Location is required")]
    [Display(Name = "Location")]
    public int? SupplierLocationId { get; set; }

    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = [];
    public IEnumerable<SelectListItem> LocationOptions { get; set; } = [];
}
