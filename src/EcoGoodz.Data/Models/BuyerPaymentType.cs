using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerPaymentType
{
    public int Id { get; set; }

    public int? BuyerId { get; set; }

    public int? PaymentType { get; set; }

    public int? Location { get; set; }

    public virtual Buyer? Buyer { get; set; }

    public virtual Location? LocationNavigation { get; set; }

    public virtual PaymentType? PaymentTypeNavigation { get; set; }
}
