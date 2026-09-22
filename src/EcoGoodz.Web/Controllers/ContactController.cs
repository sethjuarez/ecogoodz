using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class ContactController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public ContactController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? locationId)
    {
        var query = ContactRows();

        if (locationId.HasValue)
        {
            query = query.Where(c => c.Contact.Location == locationId);
        }

        var contacts = await query
            .OrderBy(c => c.Contact.LocationNavigation!.Location1)
            .ThenBy(c => c.Contact.ContactNavigation.LastName)
            .ThenBy(c => c.Contact.ContactNavigation.FirstName)
            .Select(c => new ContactListItemViewModel
            {
                Id = c.Contact.Id,
                LocationId = c.Contact.Location,
                ClientName = c.Contact.IsBuyer == true ? c.BuyerName : c.SupplierName,
                LocationName = c.Contact.LocationNavigation != null ? c.Contact.LocationNavigation.Location1 : null,
                Name = ((c.Contact.ContactNavigation.FirstName ?? string.Empty) + " " + (c.Contact.ContactNavigation.LastName ?? string.Empty)).Trim(),
                Title = c.Contact.ContactNavigation.Title,
                Email = c.Contact.ContactNavigation.Email,
                OfficePhone = c.Contact.ContactNavigation.OfficePhone,
                CellPhone = c.Contact.ContactNavigation.CellPhone,
                IsPrimaryContact = c.Contact.IsPrimaryContact == true,
                IsDockContact = c.Contact.IsDockContact == true,
                IsActive = c.Contact.IsActive,
            })
            .ToListAsync();

        return View(contacts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var contact = await ContactRows()
            .Where(c => c.Contact.Id == id)
            .Select(c => new ContactListItemViewModel
            {
                Id = c.Contact.Id,
                LocationId = c.Contact.Location,
                ClientName = c.Contact.IsBuyer == true ? c.BuyerName : c.SupplierName,
                LocationName = c.Contact.LocationNavigation != null ? c.Contact.LocationNavigation.Location1 : null,
                Name = ((c.Contact.ContactNavigation.FirstName ?? string.Empty) + " " + (c.Contact.ContactNavigation.LastName ?? string.Empty)).Trim(),
                Title = c.Contact.ContactNavigation.Title,
                Email = c.Contact.ContactNavigation.Email,
                OfficePhone = c.Contact.ContactNavigation.OfficePhone,
                CellPhone = c.Contact.ContactNavigation.CellPhone,
                IsPrimaryContact = c.Contact.IsPrimaryContact == true,
                IsDockContact = c.Contact.IsDockContact == true,
                IsActive = c.Contact.IsActive,
            })
            .FirstOrDefaultAsync();

        return contact is null ? NotFound() : View(contact);
    }

    public async Task<IActionResult> Create(int? locationId)
    {
        var model = new ContactFormViewModel { LocationId = locationId };
        if (locationId.HasValue)
        {
            await ApplyLocationAsync(model, locationId.Value);
        }

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactFormViewModel model)
    {
        await ValidateAndApplyLocationAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var contactInformation = new ContactInformation();
        ApplyContactInformation(contactInformation, model);
        contactInformation.IsActive = true;

        var hasPrimary = await _context.Contacts
            .AnyAsync(c => c.Location == model.LocationId && c.IsActive && c.IsPrimaryContact == true);

        var contact = new Contact
        {
            ContactNavigation = contactInformation,
            ClientId = model.ClientId,
            IsBuyer = model.IsBuyer,
            Location = model.LocationId,
            IsPrimaryContact = model.IsPrimaryContact || !hasPrimary,
            IsDockContact = model.IsDockContact,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Contacts.Add(contact);
        await ClearOtherPrimaryContactsAsync(contact, model.LocationId);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { locationId = model.LocationId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var contact = await _context.Contacts
            .Include(c => c.ContactNavigation)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        var model = new ContactFormViewModel
        {
            Id = contact.Id,
            ContactInformationId = contact.ContactId,
            LocationId = contact.Location,
            ClientId = contact.ClientId,
            IsBuyer = contact.IsBuyer,
            FirstName = contact.ContactNavigation.FirstName ?? string.Empty,
            LastName = contact.ContactNavigation.LastName,
            Title = contact.ContactNavigation.Title,
            Email = contact.ContactNavigation.Email,
            OfficePhone = contact.ContactNavigation.OfficePhone,
            CellPhone = contact.ContactNavigation.CellPhone,
            Address = contact.ContactNavigation.Address,
            City = contact.ContactNavigation.City,
            State = contact.ContactNavigation.State,
            Country = contact.ContactNavigation.Country,
            PinCode = contact.ContactNavigation.PinCode,
            IsPrimaryContact = contact.IsPrimaryContact == true,
            IsDockContact = contact.IsDockContact == true,
            IsActive = contact.IsActive,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContactFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        await ValidateAndApplyLocationAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var contact = await _context.Contacts
            .Include(c => c.ContactNavigation)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        ApplyContactInformation(contact.ContactNavigation, model);
        contact.ClientId = model.ClientId;
        contact.IsBuyer = model.IsBuyer;
        contact.Location = model.LocationId;
        contact.IsPrimaryContact = model.IsPrimaryContact;
        contact.IsDockContact = model.IsDockContact;
        contact.IsActive = model.IsActive;
        contact.UpdatedOn = DateTime.UtcNow;
        contact.UpdatedBy = User.GetLegacyUserId();

        await ClearOtherPrimaryContactsAsync(contact, model.LocationId);
        await EnsurePrimaryContactAsync(model.LocationId, contact);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { locationId = model.LocationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        contact.IsActive = false;
        contact.UpdatedOn = DateTime.UtcNow;
        contact.UpdatedBy = User.GetLegacyUserId();
        await EnsurePrimaryContactAsync(contact.Location, excludeContact: contact);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { locationId = contact.Location });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(int id)
    {
        var source = await _context.Contacts
            .AsNoTracking()
            .Include(c => c.ContactNavigation)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (source is null)
        {
            return NotFound();
        }

        var copy = new Contact
        {
            ClientId = source.ClientId,
            IsBuyer = source.IsBuyer,
            Location = source.Location,
            IsPrimaryContact = false,
            IsDockContact = source.IsDockContact,
            IsActive = true,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
            ContactNavigation = new ContactInformation
            {
                FirstName = source.ContactNavigation.FirstName,
                LastName = source.ContactNavigation.LastName,
                Title = source.ContactNavigation.Title,
                Email = source.ContactNavigation.Email,
                OfficePhone = source.ContactNavigation.OfficePhone,
                CellPhone = source.ContactNavigation.CellPhone,
                Address = source.ContactNavigation.Address,
                City = source.ContactNavigation.City,
                State = source.ContactNavigation.State,
                Country = source.ContactNavigation.Country,
                PinCode = source.ContactNavigation.PinCode,
                IsActive = true,
            },
        };

        _context.Contacts.Add(copy);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { locationId = source.Location });
    }

    private async Task ValidateAndApplyLocationAsync(ContactFormViewModel model)
    {
        if (!model.LocationId.HasValue)
        {
            ModelState.AddModelError(nameof(model.LocationId), "Choose a location.");
            return;
        }

        if (!await ApplyLocationAsync(model, model.LocationId.Value))
        {
            ModelState.AddModelError(nameof(model.LocationId), "Choose a valid location.");
        }
    }

    private async Task<bool> ApplyLocationAsync(ContactFormViewModel model, int locationId)
    {
        var location = await _context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId);
        if (location is null)
        {
            return false;
        }

        model.ClientId = location.ClientId;
        model.IsBuyer = location.IsBuyer;
        return true;
    }

    private async Task ClearOtherPrimaryContactsAsync(Contact contact, int? locationId)
    {
        if (contact.IsPrimaryContact != true || !locationId.HasValue)
        {
            return;
        }

        var otherPrimaryContacts = await _context.Contacts
            .Where(c => c.Location == locationId && c.Id != contact.Id && c.IsActive && c.IsPrimaryContact == true)
            .ToListAsync();

        foreach (var otherContact in otherPrimaryContacts)
        {
            otherContact.IsPrimaryContact = false;
            otherContact.UpdatedOn = DateTime.UtcNow;
            otherContact.UpdatedBy = User.GetLegacyUserId();
        }
    }

    private async Task EnsurePrimaryContactAsync(int? locationId, Contact? currentContact = null, Contact? excludeContact = null)
    {
        if (!locationId.HasValue)
        {
            return;
        }

        if (currentContact?.IsActive == true && currentContact.IsPrimaryContact == true)
        {
            return;
        }

        var hasPrimary = await _context.Contacts
            .AnyAsync(c => c.Location == locationId
                && c.IsActive
                && c.IsPrimaryContact == true
                && (excludeContact == null || c.Id != excludeContact.Id));

        if (hasPrimary)
        {
            return;
        }

        if (currentContact?.IsActive == true)
        {
            currentContact.IsPrimaryContact = true;
            return;
        }

        var replacement = await _context.Contacts
            .Where(c => c.Location == locationId && c.IsActive && (excludeContact == null || c.Id != excludeContact.Id))
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();

        if (replacement is not null)
        {
            replacement.IsPrimaryContact = true;
            replacement.UpdatedOn = DateTime.UtcNow;
            replacement.UpdatedBy = User.GetLegacyUserId();
        }
    }

    private static void ApplyContactInformation(ContactInformation contactInformation, ContactFormViewModel model)
    {
        contactInformation.FirstName = model.FirstName;
        contactInformation.LastName = model.LastName;
        contactInformation.Title = model.Title;
        contactInformation.Email = model.Email;
        contactInformation.OfficePhone = model.OfficePhone;
        contactInformation.CellPhone = model.CellPhone;
        contactInformation.Address = model.Address;
        contactInformation.City = model.City;
        contactInformation.State = model.State;
        contactInformation.Country = model.Country;
        contactInformation.PinCode = model.PinCode;
    }

    private async Task PopulateOptionsAsync(ContactFormViewModel model)
    {
        model.LocationOptions = await (
                from location in _context.Locations
                join buyer in _context.Buyers on location.ClientId equals buyer.Id into buyerJoin
                from buyer in buyerJoin.DefaultIfEmpty()
                join supplier in _context.Suppliers on location.ClientId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                where location.IsActive
                orderby location.Location1
                select new SelectListItem
                {
                    Value = location.Id.ToString(),
                    Text = (location.Location1 ?? "(Unnamed location)") + " - " + (location.IsBuyer == true ? buyer.Name : supplier.Name),
                })
            .ToListAsync();

        model.StateOptions = await _context.States
            .OrderBy(s => s.StateName)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.StateName })
            .ToListAsync();

        model.CountryOptions = await _context.Countries
            .OrderBy(c => c.CountryName)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CountryName })
            .ToListAsync();
    }

    private IQueryable<ContactRow> ContactRows() =>
        from contact in _context.Contacts
            .Include(c => c.ContactNavigation)
            .Include(c => c.LocationNavigation)
        join buyer in _context.Buyers on contact.ClientId equals buyer.Id into buyerJoin
        from buyer in buyerJoin.DefaultIfEmpty()
        join supplier in _context.Suppliers on contact.ClientId equals supplier.Id into supplierJoin
        from supplier in supplierJoin.DefaultIfEmpty()
        select new ContactRow
        {
            Contact = contact,
            BuyerName = contact.IsBuyer == true ? buyer.Name : null,
            SupplierName = contact.IsBuyer == false ? supplier.Name : null,
        };

    private sealed class ContactRow
    {
        public required Contact Contact { get; init; }
        public string? BuyerName { get; init; }
        public string? SupplierName { get; init; }
    }
}
