using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwSupplierStatus
{
    public int Id { get; set; }

    public string? Status { get; set; }

    public int ParentStatusId { get; set; }

    public string? ParentStatus { get; set; }

    public string SubStatus { get; set; } = null!;
}
