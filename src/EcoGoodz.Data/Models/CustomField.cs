using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class CustomField
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int Order { get; set; }

    public int FieldTypes { get; set; }

    public int ObjectTypes { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<UserCustomField> UserCustomFields { get; set; } = new List<UserCustomField>();
}
