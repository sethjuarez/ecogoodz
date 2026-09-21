using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class AccountManagerGoalMonth
{
    public int Id { get; set; }

    public int GoalId { get; set; }

    public byte Month { get; set; }

    public decimal? Amount { get; set; }

    public virtual AccountManagerGoal Goal { get; set; } = null!;
}
