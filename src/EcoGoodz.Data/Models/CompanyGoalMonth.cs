using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class CompanyGoalMonth
{
    public int Id { get; set; }

    public int CompanyGoalId { get; set; }

    public byte Month { get; set; }

    public decimal? GoalAmount { get; set; }

    public decimal? PriorYearActual { get; set; }

    public virtual CompanyGoal CompanyGoal { get; set; } = null!;
}
