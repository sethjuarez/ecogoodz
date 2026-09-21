using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwSupplierTrackingLoadCount
{
    public long? Id { get; set; }

    public int UserId { get; set; }

    public int UserProductId { get; set; }

    public int UserSupplierId { get; set; }

    public int ProductId { get; set; }

    public int SupplierId { get; set; }

    public int SupplierLocationId { get; set; }

    public int? BuyerLocationId { get; set; }

    public int BuyerId { get; set; }

    public string BuyerName { get; set; } = null!;

    public DateOnly? ShipmentDate { get; set; }

    public int? PendingLoad { get; set; }

    public int? ShippedLoad { get; set; }
}
