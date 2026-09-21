using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class CommunicationType
{
    public int Id { get; set; }

    public string? Type { get; set; }

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();
}
