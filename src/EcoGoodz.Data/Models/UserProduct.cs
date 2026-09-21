using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserProduct
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Product { get; set; }

    public int OrderCount { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Product ProductNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserSupplier> UserSuppliers { get; set; } = new List<UserSupplier>();
}
