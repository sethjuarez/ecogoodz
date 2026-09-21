using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class VwContactInformation
{
    public int Id { get; set; }

    public int? ClientId { get; set; }

    public bool? IsBuyer { get; set; }

    public int? Location { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? OfficePhone { get; set; }

    public string? Email { get; set; }
}
