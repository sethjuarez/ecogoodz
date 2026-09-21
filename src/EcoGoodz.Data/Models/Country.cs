using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Country
{
    public int Id { get; set; }

    public string? CountryName { get; set; }

    public string? Code { get; set; }

    public virtual ICollection<ContactInformation> ContactInformations { get; set; } = new List<ContactInformation>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual ICollection<State> States { get; set; } = new List<State>();
}
