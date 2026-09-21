using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierStatus
{
    public int Id { get; set; }

    public int? ParentStatusId { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<SupplierStatus> InverseParentStatus { get; set; } = new List<SupplierStatus>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual SupplierStatus? ParentStatus { get; set; }
}
