using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserBuyer
{
    public int Id { get; set; }

    public int OrderCount { get; set; }

    public int BuyerId { get; set; }

    public int LocationId { get; set; }

    public int UserId { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Buyer Buyer { get; set; } = null!;

    public virtual ICollection<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; } = new List<BuyerTrackingProductSupplier>();

    public virtual ICollection<BuyerTrackingProduct> BuyerTrackingProducts { get; set; } = new List<BuyerTrackingProduct>();

    public virtual Location Location { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
