using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class GoalParameter
{
    public int Id { get; set; }

    public int? AccountManagerGoal { get; set; }

    public decimal? Maximum { get; set; }

    public decimal? Excess { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public decimal? AnnualMinimum { get; set; }

    public virtual AccountManagerGoal? AccountManagerGoalNavigation { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
