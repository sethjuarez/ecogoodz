using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserInRole
{
    public int Id { get; set; }

    public int User { get; set; }

    public int Role { get; set; }

    public virtual Role RoleNavigation { get; set; } = null!;

    public virtual User UserNavigation { get; set; } = null!;
}
