using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class Location
{
    public int Id { get; set; }

    public int? Country { get; set; }

    public int? State { get; set; }

    public string? City { get; set; }

    public bool IsActive { get; set; }

    public int? ClientId { get; set; }

    public bool? IsBuyer { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Address { get; set; }

    public string? Location1 { get; set; }

    public string? PinCode { get; set; }

    public string? DockHours { get; set; }

    public decimal? Drayage1 { get; set; }

    public string? NearestPort1 { get; set; }

    public decimal? Drayage2 { get; set; }

    public string? NearestPort2 { get; set; }

    public decimal? Drayage3 { get; set; }

    public string? NearestPort3 { get; set; }

    public string? OtherStatus { get; set; }

    public string? PictureLink { get; set; }

    public int? PaymentTerms { get; set; }

    public int? NpaymentTerms { get; set; }

    public int? SupplierStatus { get; set; }

    public int? BuyerStatus { get; set; }

    public string? ScaleTickets { get; set; }

    public virtual ICollection<BuyerPaymentType> BuyerPaymentTypes { get; set; } = new List<BuyerPaymentType>();

    public virtual ICollection<BuyerProduct> BuyerProducts { get; set; } = new List<BuyerProduct>();

    public virtual BuyerStatus? BuyerStatusNavigation { get; set; }

    public virtual ICollection<BuyerSupplier> BuyerSupplierBuyerLocationNavigations { get; set; } = new List<BuyerSupplier>();

    public virtual ICollection<BuyerSupplier> BuyerSupplierSupplierLocationNavigations { get; set; } = new List<BuyerSupplier>();

    public virtual ICollection<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; } = new List<BuyerTrackingProductSupplier>();

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual Country? CountryNavigation { get; set; }

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Load> LoadBuyerLocationNavigations { get; set; } = new List<Load>();

    public virtual ICollection<Load> LoadSupplierLocationNavigations { get; set; } = new List<Load>();

    public virtual ICollection<Note> NoteBuyerLocationNavigations { get; set; } = new List<Note>();

    public virtual ICollection<Note> NoteSupplierLocationNavigations { get; set; } = new List<Note>();

    public virtual PaymentTerm? PaymentTermsNavigation { get; set; }

    public virtual State? StateNavigation { get; set; }

    public virtual ICollection<SupplierPaymentType> SupplierPaymentTypes { get; set; } = new List<SupplierPaymentType>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

    public virtual SupplierStatus? SupplierStatusNavigation { get; set; }

    public virtual ICollection<UserBuyer> UserBuyers { get; set; } = new List<UserBuyer>();

    public virtual ICollection<UserSupplierBuyerLocation> UserSupplierBuyerLocations { get; set; } = new List<UserSupplierBuyerLocation>();

    public virtual ICollection<UserSupplier> UserSuppliers { get; set; } = new List<UserSupplier>();
}
