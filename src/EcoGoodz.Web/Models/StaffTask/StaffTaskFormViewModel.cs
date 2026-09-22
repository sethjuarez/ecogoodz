using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Models.StaffTask;

public class StaffTaskFormViewModel
{
    public int Id { get; set; }
    public int? TaskId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Due date")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Assigned to")]
    public int? AssignedTo { get; set; }

    [Display(Name = "Assigned to")]
    public List<int> AssignedToIds { get; set; } = [];

    [Display(Name = "Headline")]
    public int? TaskHeadline { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> UserOptions { get; set; } = [];
    public IEnumerable<SelectListItem> HeadlineOptions { get; set; } = [];
}
