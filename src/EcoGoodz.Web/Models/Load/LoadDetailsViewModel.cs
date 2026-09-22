namespace EcoGoodz.Web.Models.Load;

public class LoadDetailsViewModel
{
    public int Id { get; set; }
    public string? BuyerName { get; set; }
    public int? BuyerId { get; set; }
    public string? BuyerLocationName { get; set; }
    public int? BuyerLocationId { get; set; }
    public string? SupplierName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierLocationName { get; set; }
    public int? SupplierLocationId { get; set; }
    public string? StatusName { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? Container { get; set; }
    public string? BuyerRef { get; set; }
    public string? SupplierRef { get; set; }
    public string? FreightCarrier { get; set; }
    public string? FreightInvoice { get; set; }
    public decimal? FreightAmountQuoted { get; set; }
    public decimal? FreightAmountBilled { get; set; }
    public string? BuyerInvoice { get; set; }
    public decimal? BuyerInvoiceAmount { get; set; }
    public DateTime? BuyerInvoiceDate { get; set; }
    public string? SupplierInvoice { get; set; }
    public decimal? SupplierInvoiceAmount { get; set; }
    public DateTime? SupplierInvoiceDate { get; set; }
    public string? BuyerNotes { get; set; }
    public string? SupplierNotes { get; set; }
    public string? FreightNotes { get; set; }
    public bool IsComplete { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreateOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ProductLineCount { get; set; }
    public IReadOnlyList<LoadProductLineViewModel> ProductLines { get; set; } = [];
}

public class LoadProductLineViewModel
{
    public int SupplierProductId { get; set; }
    public string? SupplierName { get; set; }
    public string? ProductName { get; set; }
    public string? PackagingName { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateTime? EffectiveDate { get; set; }
}
