using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Load
{
    public int Id { get; set; }

    public int? LoadStatus { get; set; }

    public int? Supplier { get; set; }

    public int? Buyer { get; set; }

    public int? BuyerLocation { get; set; }

    public int? SupplierLocation { get; set; }

    public DateTime? ShipmentDate { get; set; }

    public string? SupplierRef { get; set; }

    public string? Container { get; set; }

    public string? SupplierInvoice { get; set; }

    public int? SupplierInvoiceLbs { get; set; }

    public string? BuyerRef { get; set; }

    public string? BuyerInvoice { get; set; }

    public DateTime? CreateOn { get; set; }

    public DateTime? BuyerInvoiceDate { get; set; }

    public string? FreightCarrier { get; set; }

    public string? FreightInvoice { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public decimal? FreightAmountBilled { get; set; }

    public decimal? FreightAmountQuoted { get; set; }

    public bool? IsBuyersTrucking { get; set; }

    public DateTime? SupplierInvoiceDate { get; set; }

    public decimal? BuyerInvoiceAmount { get; set; }

    public decimal? SupplierInvoiceAmount { get; set; }

    public int? SupplierAccountMgr { get; set; }

    public int? BuyerAccountMgr { get; set; }

    public bool? IsComplete { get; set; }

    public string? SupplierNotes { get; set; }

    public string? BuyerNotes { get; set; }

    public string? FreightNotes { get; set; }

    public bool IsSalesTracker { get; set; }

    public DateTime? BookingDate { get; set; }

    public virtual User? BuyerAccountMgrNavigation { get; set; }

    public virtual Location? BuyerLocationNavigation { get; set; }

    public virtual Buyer? BuyerNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<GrossProfitProjectionLoad> GrossProfitProjectionLoads { get; set; } = new List<GrossProfitProjectionLoad>();

    public virtual ICollection<LoadProduct> LoadProducts { get; set; } = new List<LoadProduct>();

    public virtual LoadStatus? LoadStatusNavigation { get; set; }

    public virtual User? SupplierAccountMgrNavigation { get; set; }

    public virtual Location? SupplierLocationNavigation { get; set; }

    public virtual Supplier? SupplierNavigation { get; set; }
}
