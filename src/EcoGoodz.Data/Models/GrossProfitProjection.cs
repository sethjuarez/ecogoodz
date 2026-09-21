using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class GrossProfitProjection
{
    public int Id { get; set; }

    public int LoadId { get; set; }

    public string? LoadIds { get; set; }

    public decimal? SupplierRate { get; set; }

    public decimal? BuyerRate { get; set; }

    public string? Scenario { get; set; }

    public decimal? Estimate { get; set; }

    public DateTime? CreatedTime { get; set; }

    public decimal? Averagelbs { get; set; }

    public virtual ICollection<GrossProfitProjectionLoad> GrossProfitProjectionLoads { get; set; } = new List<GrossProfitProjectionLoad>();
}
