using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Load;

public class LoadFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Buyer")]
    public int? Buyer { get; set; }

    [Required]
    [Display(Name = "Supplier")]
    public int? Supplier { get; set; }

    [Display(Name = "Buyer location")]
    public int? BuyerLocation { get; set; }

    [Display(Name = "Supplier location")]
    public int? SupplierLocation { get; set; }

    [Display(Name = "Status")]
    public int? LoadStatus { get; set; }

    [Display(Name = "Shipment date")]
    [DataType(DataType.Date)]
    public DateTime? ShipmentDate { get; set; }

    public string? Container { get; set; }

    [Display(Name = "Buyer reference")]
    public string? BuyerRef { get; set; }

    [Display(Name = "Supplier reference")]
    public string? SupplierRef { get; set; }

    [Display(Name = "Freight carrier")]
    public string? FreightCarrier { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Product lines")]
    public List<int> SupplierProductIds { get; set; } = [];

    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BuyerLocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierLocationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SupplierProductOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
