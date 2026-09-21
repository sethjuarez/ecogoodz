using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class TaskItem
{
    public int Id { get; set; }

    public string? Description { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? Duedate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AssignTask> AssignTasks { get; set; } = new List<AssignTask>();

    public virtual User? CreatedByNavigation { get; set; }
}
