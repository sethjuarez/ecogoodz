using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class PaymentType
{
    public int Id { get; set; }

    public string? Type { get; set; }

    public virtual ICollection<BuyerPaymentType> BuyerPaymentTypes { get; set; } = new List<BuyerPaymentType>();

    public virtual ICollection<SupplierPaymentType> SupplierPaymentTypes { get; set; } = new List<SupplierPaymentType>();
}
