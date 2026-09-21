using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Favorite
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Location { get; set; }

    public bool IsBuyer { get; set; }

    public virtual Location LocationNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
