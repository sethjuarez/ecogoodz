using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class GrossProfitProjectionLoad
{
    public int Id { get; set; }

    public int GrossProfitProjectionId { get; set; }

    public int LoadId { get; set; }

    public virtual GrossProfitProjection GrossProfitProjection { get; set; } = null!;

    public virtual Load Load { get; set; } = null!;
}
