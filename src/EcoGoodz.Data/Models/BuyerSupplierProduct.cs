using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerSupplierProduct
{
    public int Id { get; set; }

    public int? BuyerSupplierId { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public int? SupplierProduct { get; set; }

    public virtual ICollection<BuyerProductHistory> BuyerProductHistories { get; set; } = new List<BuyerProductHistory>();

    public virtual ICollection<BuyerProductRate> BuyerProductRates { get; set; } = new List<BuyerProductRate>();

    public virtual BuyerSupplier? BuyerSupplier { get; set; }

    public virtual SupplierProduct? SupplierProductNavigation { get; set; }
}
