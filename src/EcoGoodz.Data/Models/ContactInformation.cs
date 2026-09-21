using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class ContactInformation
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Title { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public string? PinCode { get; set; }

    public string? OfficePhone { get; set; }

    public string? CellPhone { get; set; }

    public int? State { get; set; }

    public string? City { get; set; }

    public int? Country { get; set; }

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual Country? CountryNavigation { get; set; }

    public virtual State? StateNavigation { get; set; }
}
