using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierBuyerStatus
{
    public int Id { get; set; }

    public int? ParentStatusId { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<BuyerSupplier> BuyerSuppliers { get; set; } = new List<BuyerSupplier>();
}
