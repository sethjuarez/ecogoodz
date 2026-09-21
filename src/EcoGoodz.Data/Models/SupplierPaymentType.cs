using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class SupplierPaymentType
{
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    public int? PaymentType { get; set; }

    public int? Location { get; set; }

    public virtual Location? LocationNavigation { get; set; }

    public virtual PaymentType? PaymentTypeNavigation { get; set; }

    public virtual Supplier? Supplier { get; set; }
}
