namespace EcoGoodz.Web.Models.StaffTask;

public class StaffTaskBoardViewModel
{
    public IReadOnlyList<StaffTaskBoardHeadlineViewModel> Headlines { get; set; } = [];
    public StaffTaskBoardHeadlineViewModel? SelectedHeadline { get; set; }
    public IReadOnlyList<StaffTaskBoardTaskViewModel> Tasks { get; set; } = [];
}

public class StaffTaskBoardHeadlineViewModel
{
    public int Id { get; set; }
    public string Headline { get; set; } = string.Empty;
    public int OpenTaskCount { get; set; }
    public int UnreadTaskCount { get; set; }
}

public class StaffTaskBoardTaskViewModel
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsDone { get; set; }
    public bool IsRead { get; set; }
    public bool IsCreatedByCurrentUser { get; set; }
}
