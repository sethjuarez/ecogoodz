using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Contact
{
    public int Id { get; set; }

    public int ContactId { get; set; }

    public int? ClientId { get; set; }

    public bool? IsPrimaryContact { get; set; }

    public bool? IsBuyer { get; set; }

    public bool IsActive { get; set; }

    public int? Location { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsDockContact { get; set; }

    public virtual ContactInformation ContactNavigation { get; set; } = null!;

    public virtual Location? LocationNavigation { get; set; }
}
