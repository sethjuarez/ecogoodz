using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwSupplier
{
    public long? RowId { get; set; }

    public int Id { get; set; }

    public int Location { get; set; }

    public string? Name { get; set; }

    public string? LocationName { get; set; }

    public string? Status { get; set; }

    public string SubStatus { get; set; } = null!;

    public string? Contact { get; set; }

    public string? Phone { get; set; }

    public string Email { get; set; } = null!;

    public int? PaymentTerms { get; set; }

    public string? PaymentTermsName { get; set; }

    public bool? IsFavorite { get; set; }

    public int? Country { get; set; }

    public int? State { get; set; }

    public int StatusId { get; set; }

    public int ParentStatusId { get; set; }
}
