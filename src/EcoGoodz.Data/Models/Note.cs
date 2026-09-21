using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Note
{
    public int Id { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsBuyer { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public int? BuyerLocation { get; set; }

    public int? SupplierLocation { get; set; }

    public bool IsProduct { get; set; }

    public int? Product { get; set; }

    public virtual Location? BuyerLocationNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual SupplierProduct? ProductNavigation { get; set; }

    public virtual Location? SupplierLocationNavigation { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
