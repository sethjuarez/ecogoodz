using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class TaskHeadline
{
    public int Id { get; set; }

    public string? Headline { get; set; }

    public int? UserId { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<AssignTask> AssignTasks { get; set; } = new List<AssignTask>();

    public virtual User? User { get; set; }
}
