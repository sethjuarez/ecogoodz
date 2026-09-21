using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserSupplier
{
    public int Id { get; set; }

    public int UserProductId { get; set; }

    public int OrderCount { get; set; }

    public int SupplierId { get; set; }

    public int LocationId { get; set; }

    public string? BuyerLocationIds { get; set; }

    public string? SupplierNote { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public int? NoteUpdatedBy { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual User? NoteUpdatedByNavigation { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual UserProduct UserProduct { get; set; } = null!;

    public virtual ICollection<UserSupplierBuyerLocation> UserSupplierBuyerLocations { get; set; } = new List<UserSupplierBuyerLocation>();
}
