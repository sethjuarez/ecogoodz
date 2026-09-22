using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.StaffTask;

public class StaffTaskHeadlineListItemViewModel
{
    public int Id { get; set; }

    public string Headline { get; set; } = string.Empty;

    public int OpenTaskCount { get; set; }
}

public class StaffTaskHeadlineFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Headline { get; set; } = string.Empty;
}
