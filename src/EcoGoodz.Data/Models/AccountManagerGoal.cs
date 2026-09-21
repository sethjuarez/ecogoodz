using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class AccountManagerGoal
{
    public int Id { get; set; }

    public int? User { get; set; }

    public int? GoalType { get; set; }

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

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Year { get; set; }

    public decimal? CommissionPer { get; set; }

    public virtual ICollection<AccountManagerGoalMonth> AccountManagerGoalMonths { get; set; } = new List<AccountManagerGoalMonth>();

    public virtual ICollection<GoalParameter> GoalParameters { get; set; } = new List<GoalParameter>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual User? UserNavigation { get; set; }
}
