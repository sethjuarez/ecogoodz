using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierProduct
{
    public int Id { get; set; }

    public int? Supplier { get; set; }

    public int? Location { get; set; }

    public int? Product { get; set; }

    public int? Packaging { get; set; }

    public bool IsActive { get; set; }

    public decimal? PackagingPrice { get; set; }

    public string? OtherPackaging { get; set; }

    public int? Volume { get; set; }

    public int? FrequencyPackageType { get; set; }

    public int? Frequency { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<BuyerSupplierProduct> BuyerSupplierProducts { get; set; } = new List<BuyerSupplierProduct>();

    public virtual FrequencyPackageType? FrequencyPackageTypeNavigation { get; set; }

    public virtual ICollection<LoadProduct> LoadProducts { get; set; } = new List<LoadProduct>();

    public virtual Location? LocationNavigation { get; set; }

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual PackageType? PackagingNavigation { get; set; }

    public virtual Product? ProductNavigation { get; set; }

    public virtual Supplier? SupplierNavigation { get; set; }

    public virtual ICollection<SupplierProductHistory> SupplierProductHistories { get; set; } = new List<SupplierProductHistory>();

    public virtual ICollection<SupplierProductRate> SupplierProductRates { get; set; } = new List<SupplierProductRate>();
}
