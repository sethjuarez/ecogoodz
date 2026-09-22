using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.Communication;

public class CommunicationFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Client type")]
    public string ClientType { get; set; } = "Buyer";

    [Display(Name = "Buyer")]
    public int? BuyerId { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Communication type")]
    public int? CommunicationType { get; set; }

    [StringLength(100)]
    [Display(Name = "Other type")]
    public string? OtherType { get; set; }

    [Required]
    [StringLength(2000)]
    public string Note { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; } = DateTime.Today;

    public IEnumerable<SelectListItem> ClientTypeOptions { get; set; } = [];
    public IEnumerable<SelectListItem> BuyerOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = [];
    public IEnumerable<SelectListItem> CommunicationTypeOptions { get; set; } = [];
}
