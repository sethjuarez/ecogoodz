using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class State
{
    public int Id { get; set; }

    public string? StateName { get; set; }

    public string? Code { get; set; }

    public int? CountryId { get; set; }

    public virtual ICollection<ContactInformation> ContactInformations { get; set; } = new List<ContactInformation>();

    public virtual Country? Country { get; set; }

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
}
