using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerProduct
{
    public int Id { get; set; }

    public int? Location { get; set; }

    public int? Product { get; set; }

    public string? OtherProduct { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public int? Buyer { get; set; }

    public virtual Buyer? BuyerNavigation { get; set; }

    public virtual ICollection<BuyerProductPackaging> BuyerProductPackagings { get; set; } = new List<BuyerProductPackaging>();

    public virtual Location? LocationNavigation { get; set; }

    public virtual Product? ProductNavigation { get; set; }
}
