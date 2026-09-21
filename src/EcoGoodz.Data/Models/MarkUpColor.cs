using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class MarkUpColor
{
    public int Id { get; set; }

    public string? Color { get; set; }

    public string? ColorCode { get; set; }

    public virtual ICollection<ProductMarkUpColor> ProductMarkUpColors { get; set; } = new List<ProductMarkUpColor>();
}
