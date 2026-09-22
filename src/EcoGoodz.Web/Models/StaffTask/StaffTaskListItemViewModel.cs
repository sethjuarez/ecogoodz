namespace EcoGoodz.Web.Models.StaffTask;

public class StaffTaskListItemViewModel
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? AssignedToName { get; set; }
    public string? Headline { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneDate { get; set; }
    public bool IsActive { get; set; }
}
