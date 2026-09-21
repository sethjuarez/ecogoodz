using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerProductRate
{
    public int Id { get; set; }

    public decimal? Price { get; set; }

    public int? BuyerSupplierProductId { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? UserId { get; set; }

    public bool IsActive { get; set; }

    public virtual BuyerSupplierProduct? BuyerSupplierProduct { get; set; }

    public virtual User? User { get; set; }
}
