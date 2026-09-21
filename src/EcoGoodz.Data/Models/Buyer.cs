using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Buyer
{
    public int Id { get; set; }

    public int? AccountManager { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public virtual User? AccountManagerNavigation { get; set; }

    public virtual ICollection<BuyerPaymentType> BuyerPaymentTypes { get; set; } = new List<BuyerPaymentType>();

    public virtual ICollection<BuyerProduct> BuyerProducts { get; set; } = new List<BuyerProduct>();

    public virtual ICollection<BuyerSupplier> BuyerSuppliers { get; set; } = new List<BuyerSupplier>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Load> Loads { get; set; } = new List<Load>();

    public virtual ICollection<UserBuyer> UserBuyers { get; set; } = new List<UserBuyer>();
}
