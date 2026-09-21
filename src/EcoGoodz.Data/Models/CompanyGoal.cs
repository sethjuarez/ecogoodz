using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class CompanyGoal
{
    public int Id { get; set; }

    public decimal? M1 { get; set; }

    public decimal? M2 { get; set; }

    public decimal? M3 { get; set; }

    public decimal? M4 { get; set; }

    public decimal? M5 { get; set; }

    public decimal? M6 { get; set; }

    public decimal? M7 { get; set; }

    public decimal? M8 { get; set; }

    public decimal? M9 { get; set; }

    public decimal? M10 { get; set; }

    public decimal? M11 { get; set; }

    public decimal? M12 { get; set; }

    public string? Year { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public decimal? P1 { get; set; }

    public decimal? P2 { get; set; }

    public decimal? P3 { get; set; }

    public decimal? P4 { get; set; }

    public decimal? P5 { get; set; }

    public decimal? P6 { get; set; }

    public decimal? P7 { get; set; }

    public decimal? P8 { get; set; }

    public decimal? P9 { get; set; }

    public decimal? P10 { get; set; }

    public decimal? P11 { get; set; }

    public decimal? P12 { get; set; }

    public virtual ICollection<CompanyGoalMonth> CompanyGoalMonths { get; set; } = new List<CompanyGoalMonth>();

    public virtual User? UpdatedByNavigation { get; set; }
}
