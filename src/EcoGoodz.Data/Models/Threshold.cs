using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Threshold
{
    public int Id { get; set; }

    public decimal Percentage { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }
}
