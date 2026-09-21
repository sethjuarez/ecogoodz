using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class BuyerSupplierHistory
{
    public int Id { get; set; }

    public int? NewStatus { get; set; }

    public int? OldStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public virtual User? User { get; set; }
}
