using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerSupplier
{
    public int Id { get; set; }

    public int? Buyer { get; set; }

    public int? Supplier { get; set; }

    public int? BuyerLocation { get; set; }

    public int? SupplierLocation { get; set; }

    public int? Status { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Location? BuyerLocationNavigation { get; set; }

    public virtual Buyer? BuyerNavigation { get; set; }

    public virtual ICollection<BuyerSupplierProduct> BuyerSupplierProducts { get; set; } = new List<BuyerSupplierProduct>();

    public virtual SupplierBuyerStatus? StatusNavigation { get; set; }

    public virtual Location? SupplierLocationNavigation { get; set; }

    public virtual Supplier? SupplierNavigation { get; set; }
}
