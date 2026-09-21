using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class FrequencyPackageType
{
    public int Id { get; set; }

    public string? Type { get; set; }

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
}
