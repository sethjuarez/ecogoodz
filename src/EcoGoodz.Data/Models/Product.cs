using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Product
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public int? IdParent { get; set; }

    public virtual ICollection<BuyerProduct> BuyerProducts { get; set; } = new List<BuyerProduct>();

    public virtual ICollection<BuyerTrackingProduct> BuyerTrackingProducts { get; set; } = new List<BuyerTrackingProduct>();

    public virtual ICollection<ProductMarkUpColor> ProductMarkUpColors { get; set; } = new List<ProductMarkUpColor>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

    public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();
}
