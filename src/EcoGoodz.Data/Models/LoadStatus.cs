using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class LoadStatus
{
    public int Id { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Load> Loads { get; set; } = new List<Load>();
}
