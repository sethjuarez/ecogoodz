using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class AssignTask
{
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public int? AssignedTo { get; set; }

    public int? TaskHeadline { get; set; }

    public DateTime? DoneDate { get; set; }

    public bool IsDone { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public bool IsRead { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual TaskItem? Task { get; set; }

    public virtual TaskHeadline? TaskHeadlineNavigation { get; set; }
}
