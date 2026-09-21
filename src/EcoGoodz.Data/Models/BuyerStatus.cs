using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerStatus
{
    public int Id { get; set; }

    public string? Status { get; set; }

    public int? ParentStatusId { get; set; }

    public virtual ICollection<BuyerStatus> InverseParentStatus { get; set; } = new List<BuyerStatus>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual BuyerStatus? ParentStatus { get; set; }
}
