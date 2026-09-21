using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserSupplierBuyerLocation
{
    public int Id { get; set; }

    public int UserSupplierId { get; set; }

    public int LocationId { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual UserSupplier UserSupplier { get; set; } = null!;
}
