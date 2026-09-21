using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwSupplierRate
{
    public long? Id { get; set; }

    public int SupplierId { get; set; }

    public int LocationId { get; set; }

    public int? Product { get; set; }

    public int? Packaging { get; set; }

    public int SupplierProductId { get; set; }

    public decimal? Rate { get; set; }

    public DateTime? CreatedDate { get; set; }
}
