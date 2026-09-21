using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class PackageType
{
    public int Id { get; set; }

    public string? Type { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<BuyerProductPackaging> BuyerProductPackagings { get; set; } = new List<BuyerProductPackaging>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
}
