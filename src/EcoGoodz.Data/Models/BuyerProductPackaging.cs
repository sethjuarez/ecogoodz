using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerProductPackaging
{
    public int Id { get; set; }

    public int BuyerProduct { get; set; }

    public int? Packaging { get; set; }

    public string? OtherPackaging { get; set; }

    public virtual BuyerProduct BuyerProductNavigation { get; set; } = null!;

    public virtual PackageType? PackagingNavigation { get; set; }
}
