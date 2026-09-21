using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierProductHistory
{
    public int Id { get; set; }

    public int? SupplierProductId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? UserId { get; set; }

    public decimal? NewPrice { get; set; }

    public decimal? OldPrice { get; set; }

    public DateTime? NewEffectiveDate { get; set; }

    public DateTime? OldEffectiveDate { get; set; }

    public string? Action { get; set; }

    public virtual SupplierProduct? SupplierProduct { get; set; }

    public virtual User? User { get; set; }
}
