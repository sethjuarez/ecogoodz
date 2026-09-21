using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class LoadProduct
{
    public int Id { get; set; }

    public int Product { get; set; }

    public int Load { get; set; }

    public virtual Load LoadNavigation { get; set; } = null!;

    public virtual SupplierProduct ProductNavigation { get; set; } = null!;
}
