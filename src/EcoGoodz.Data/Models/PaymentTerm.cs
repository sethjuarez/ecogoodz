using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class PaymentTerm
{
    public int Id { get; set; }

    public string? Term { get; set; }

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
}
