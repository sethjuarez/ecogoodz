using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerTrackingProduct
{
    public int Id { get; set; }

    public int UserBuyerId { get; set; }

    public int ProductId { get; set; }

    public int Order { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; } = new List<BuyerTrackingProductSupplier>();

    public virtual Product Product { get; set; } = null!;

    public virtual UserBuyer UserBuyer { get; set; } = null!;
}
