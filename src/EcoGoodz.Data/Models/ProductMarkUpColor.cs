using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class ProductMarkUpColor
{
    public int Id { get; set; }

    public int? MarkUpColor { get; set; }

    public decimal? MinRate { get; set; }

    public decimal? MaxRate { get; set; }

    public int Product { get; set; }

    public virtual MarkUpColor? MarkUpColorNavigation { get; set; }

    public virtual Product ProductNavigation { get; set; } = null!;
}
