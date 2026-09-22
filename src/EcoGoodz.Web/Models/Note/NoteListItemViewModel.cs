namespace EcoGoodz.Web.Models.Note;

public class NoteListItemViewModel
{
    public int Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public string? ProductName { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsActive { get; set; }
}
