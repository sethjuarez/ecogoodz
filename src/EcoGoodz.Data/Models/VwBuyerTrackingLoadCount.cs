using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwBuyerTrackingLoadCount
{
    public long? Id { get; set; }

    public int UserId { get; set; }

    public int UserBuyerId { get; set; }

    public int BuyerProductId { get; set; }

    public int BuyerId { get; set; }

    public int ProductId { get; set; }

    public int BuyerLocationId { get; set; }

    public int SupplierId { get; set; }

    public int SupplierLocationId { get; set; }

    public DateOnly? Shipmentdate { get; set; }

    public int? Pendingload { get; set; }

    public int? Shippedload { get; set; }
}
