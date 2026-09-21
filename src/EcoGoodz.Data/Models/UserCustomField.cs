using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class UserCustomField
{
    public int Id { get; set; }

    public int? User { get; set; }

    public int CustomField { get; set; }

    public string? Value { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual CustomField CustomFieldNavigation { get; set; } = null!;

    public virtual User? UserNavigation { get; set; }
}
