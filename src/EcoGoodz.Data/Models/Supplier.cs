using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Supplier
{
    public int Id { get; set; }

    public int? AccountManager { get; set; }

    public string? Name { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual User? AccountManagerNavigation { get; set; }

    public virtual ICollection<BuyerSupplier> BuyerSuppliers { get; set; } = new List<BuyerSupplier>();

    public virtual ICollection<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; } = new List<BuyerTrackingProductSupplier>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Load> Loads { get; set; } = new List<Load>();

    public virtual ICollection<SupplierPaymentType> SupplierPaymentTypes { get; set; } = new List<SupplierPaymentType>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

    public virtual ICollection<UserSupplier> UserSuppliers { get; set; } = new List<UserSupplier>();
}
