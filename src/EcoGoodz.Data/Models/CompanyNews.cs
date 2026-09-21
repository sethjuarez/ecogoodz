using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class CompanyNews
{
    public int Id { get; set; }

    public string? News { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public int? NewsId { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<CompanyNews> InverseNewsNavigation { get; set; } = new List<CompanyNews>();

    public virtual CompanyNews? NewsNavigation { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
