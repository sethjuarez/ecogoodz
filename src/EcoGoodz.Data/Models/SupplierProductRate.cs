using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierProductRate
{
    public int Id { get; set; }

    public int? SupplierProductId { get; set; }

    public decimal? Price { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? UserId { get; set; }

    public bool IsActive { get; set; }

    public virtual SupplierProduct? SupplierProduct { get; set; }

    public virtual User? User { get; set; }
}
