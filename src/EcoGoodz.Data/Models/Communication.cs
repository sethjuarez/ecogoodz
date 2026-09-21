using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Communication
{
    public int Id { get; set; }

    public int? CommunicationType { get; set; }

    public string? Note { get; set; }

    public DateTime? Date { get; set; }

    public bool IsActive { get; set; }

    public int? ClientId { get; set; }

    public bool? IsBuyer { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public string? OtherType { get; set; }

    public int TrackingId { get; set; }

    public virtual CommunicationType? CommunicationTypeNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
