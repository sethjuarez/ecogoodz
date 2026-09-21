using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class ProductV2
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public int? IdParent { get; set; }
}
