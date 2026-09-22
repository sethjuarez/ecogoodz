using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Location;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class LocationController : PagedListController<LocationController.LocationRow, LocationListItemViewModel>
{
    private const string BuyerClientType = "Buyer";
    private const string SupplierClientType = "Supplier";

    public LocationController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<LocationRow> GetBaseQuery() =>
        from location in Context.Locations
        join buyer in Context.Buyers on location.ClientId equals buyer.Id into buyerJoin
        from buyer in buyerJoin.DefaultIfEmpty()
        join supplier in Context.Suppliers on location.ClientId equals supplier.Id into supplierJoin
        from supplier in supplierJoin.DefaultIfEmpty()
        select new LocationRow
        {
            Location = location,
            BuyerName = location.IsBuyer == true ? buyer.Name : null,
            SupplierName = location.IsBuyer == false ? supplier.Name : null,
        };

    protected override IQueryable<LocationRow> ApplySearch(IQueryable<LocationRow> query, string searchTerm) =>
        query.Where(r =>
            (r.Location.Location1 != null && r.Location.Location1.Contains(searchTerm))
            || (r.Location.City != null && r.Location.City.Contains(searchTerm))
            || (r.Location.Address != null && r.Location.Address.Contains(searchTerm))
            || (r.Location.StateNavigation != null && r.Location.StateNavigation.StateName != null && r.Location.StateNavigation.StateName.Contains(searchTerm))
            || (r.Location.CountryNavigation != null && r.Location.CountryNavigation.CountryName != null && r.Location.CountryNavigation.CountryName.Contains(searchTerm))
            || (r.BuyerName != null && r.BuyerName.Contains(searchTerm))
            || (r.SupplierName != null && r.SupplierName.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<LocationRow, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<LocationRow, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = r => EF.Property<string>(r.Location, "LocationSort"),
            ["client"] = r => r.Location.IsBuyer == true ? r.BuyerName : r.SupplierName,
            ["city"] = r => EF.Property<string>(r.Location, "CitySort"),
            ["state"] = r => r.Location.StateNavigation != null ? r.Location.StateNavigation.StateName : null,
            ["country"] = r => r.Location.CountryNavigation != null ? r.Location.CountryNavigation.CountryName : null,
            ["active"] = r => r.Location.IsActive,
        };

    protected override string DefaultSortColumn => "name";

    protected override Expression<Func<LocationRow, LocationListItemViewModel>> ProjectionExpression =>
        r => new LocationListItemViewModel
        {
            Id = r.Location.Id,
            Name = r.Location.Location1 ?? string.Empty,
            ClientName = r.Location.IsBuyer == true ? r.BuyerName : r.SupplierName,
            City = r.Location.City,
            StateName = r.Location.StateNavigation != null ? r.Location.StateNavigation.StateName : null,
            CountryName = r.Location.CountryNavigation != null ? r.Location.CountryNavigation.CountryName : null,
            IsActive = r.Location.IsActive,
        };

    public sealed class LocationRow
    {
        public required Data.Models.Location Location { get; init; }
        public string? BuyerName { get; init; }
        public string? SupplierName { get; init; }
    }

    public override async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(sort) || desc)
        {
            return await base.Index(search, sort, desc, page, pageSize);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;

        var totalCount = await Context.Locations.AsNoTracking().CountAsync();
        var pageLocations = Context.Locations
            .AsNoTracking()
            .OrderBy(location => location.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
                from location in pageLocations
                join buyer in Context.Buyers.AsNoTracking() on location.ClientId equals buyer.Id into buyerJoin
                from buyer in buyerJoin.DefaultIfEmpty()
                join supplier in Context.Suppliers.AsNoTracking() on location.ClientId equals supplier.Id into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                join state in Context.States.AsNoTracking() on location.State equals state.Id into stateJoin
                from state in stateJoin.DefaultIfEmpty()
                join country in Context.Countries.AsNoTracking() on location.Country equals country.Id into countryJoin
                from country in countryJoin.DefaultIfEmpty()
                orderby location.Id
                select new LocationListItemViewModel
                {
                    Id = location.Id,
                    Name = location.Location1 ?? string.Empty,
                    ClientName = location.IsBuyer == true
                        ? buyer.Name
                        : location.IsBuyer == false ? supplier.Name : null,
                    City = location.City,
                    StateName = state.StateName,
                    CountryName = country.CountryName,
                    IsActive = location.IsActive,
                })
            .ToListAsync();

        return View("Index", new PagedResult<LocationListItemViewModel>
        {
            Items = items,
            Page = new PageInfo
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search,
            },
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetLegacyUserId();
        var location = await GetBaseQuery()
            .Where(r => r.Location.Id == id)
            .Select(r => new LocationDetailsViewModel
            {
                Id = r.Location.Id,
                Name = r.Location.Location1 ?? string.Empty,
                ClientType = r.Location.IsBuyer == true ? BuyerClientType : r.Location.IsBuyer == false ? SupplierClientType : null,
                ClientName = r.Location.IsBuyer == true ? r.BuyerName : r.SupplierName,
                Address = r.Location.Address,
                City = r.Location.City,
                StateName = r.Location.StateNavigation != null ? r.Location.StateNavigation.StateName : null,
                CountryName = r.Location.CountryNavigation != null ? r.Location.CountryNavigation.CountryName : null,
                PinCode = r.Location.PinCode,
                DockHours = r.Location.DockHours,
                PaymentTermsName = r.Location.PaymentTermsNavigation != null ? r.Location.PaymentTermsNavigation.Term : null,
                BuyerStatusName = r.Location.BuyerStatusNavigation != null ? r.Location.BuyerStatusNavigation.Status : null,
                SupplierStatusName = r.Location.SupplierStatusNavigation != null ? r.Location.SupplierStatusNavigation.Status : null,
                IsActive = r.Location.IsActive,
                CreateOn = r.Location.CreateOn,
                UpdatedOn = r.Location.UpdatedOn,
                BuyerProductCount = r.Location.BuyerProducts.Count,
                SupplierProductCount = r.Location.SupplierProducts.Count,
                LoadCount = r.Location.LoadBuyerLocationNavigations.Count + r.Location.LoadSupplierLocationNavigations.Count,
                MatchCount = r.Location.BuyerSupplierBuyerLocationNavigations.Count + r.Location.BuyerSupplierSupplierLocationNavigations.Count,
                IsFavorite = userId.HasValue
                    && r.Location.Favorites.Any(favorite =>
                        favorite.UserId == userId.Value
                        && favorite.IsBuyer == (r.Location.IsBuyer == true)),
            })
            .FirstOrDefaultAsync();

        if (location is null)
        {
            return NotFound();
        }

        return View(location);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var location = await Context.Locations
            .AsNoTracking()
            .Where(location => location.Id == id)
            .Select(location => new { location.Id, location.IsBuyer })
            .FirstOrDefaultAsync();
        if (location is null)
        {
            return NotFound();
        }

        if (location.IsBuyer is null)
        {
            return BadRequest();
        }

        var isBuyer = location.IsBuyer.Value;
        var favorites = await Context.Favorites
            .Where(favorite =>
            favorite.UserId == userId.Value
            && favorite.Location == location.Id
            && favorite.IsBuyer == isBuyer)
            .ToListAsync();
        if (favorites.Count == 0)
        {
            Context.Favorites.Add(new Data.Models.Favorite
            {
                UserId = userId.Value,
                Location = location.Id,
                IsBuyer = isBuyer,
            });
        }
        else
        {
            Context.Favorites.RemoveRange(favorites);
        }

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Create(int? copyFromId)
    {
        var model = new LocationFormViewModel();
        if (copyFromId.HasValue)
        {
            var source = await Context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == copyFromId.Value);
            if (source is null)
            {
                return NotFound();
            }

            model = new LocationFormViewModel
            {
                CopyLocationId = source.Id,
                CopyLocationName = source.Location1,
                ClientType = source.IsBuyer == true ? BuyerClientType : source.IsBuyer == false ? SupplierClientType : null,
                BuyerClientId = source.IsBuyer == true ? source.ClientId : null,
                SupplierClientId = source.IsBuyer == false ? source.ClientId : null,
                Country = source.Country,
                State = source.State,
                City = source.City,
                Address = source.Address,
                PinCode = source.PinCode,
                DockHours = source.DockHours,
                PaymentTerms = source.PaymentTerms,
                BuyerStatus = source.BuyerStatus,
                SupplierStatus = source.SupplierStatus,
                IsActive = true,
            };
        }

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LocationFormViewModel model)
    {
        var copySource = await ValidateCopySourceAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var location = new Data.Models.Location
        {
            Location1 = model.Name,
            IsBuyer = GetIsBuyer(model.ClientType),
            ClientId = GetClientId(model),
            Country = model.Country,
            State = model.State,
            City = model.City,
            Address = model.Address,
            PinCode = model.PinCode,
            DockHours = model.DockHours,
            PaymentTerms = model.PaymentTerms,
            NpaymentTerms = copySource?.NpaymentTerms,
            BuyerStatus = model.BuyerStatus,
            SupplierStatus = model.SupplierStatus,
            Drayage1 = copySource?.Drayage1,
            Drayage2 = copySource?.Drayage2,
            Drayage3 = copySource?.Drayage3,
            NearestPort1 = copySource?.NearestPort1,
            NearestPort2 = copySource?.NearestPort2,
            NearestPort3 = copySource?.NearestPort3,
            OtherStatus = copySource?.OtherStatus,
            PictureLink = copySource?.PictureLink,
            ScaleTickets = copySource?.ScaleTickets,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.Locations.Add(location);

        if (Context.Database.IsRelational())
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();
            await Context.SaveChangesAsync();
            await CopyLocationChildrenAsync(copySource, location);
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        else
        {
            await Context.SaveChangesAsync();
            await CopyLocationChildrenAsync(copySource, location);
            await Context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var location = await Context.Locations.FindAsync(id);
        if (location is null)
        {
            return NotFound();
        }

        var model = new LocationFormViewModel
        {
            Id = location.Id,
            Name = location.Location1 ?? string.Empty,
            ClientType = location.IsBuyer == true ? BuyerClientType : location.IsBuyer == false ? SupplierClientType : null,
            BuyerClientId = location.IsBuyer == true ? location.ClientId : null,
            SupplierClientId = location.IsBuyer == false ? location.ClientId : null,
            Country = location.Country,
            State = location.State,
            City = location.City,
            Address = location.Address,
            PinCode = location.PinCode,
            DockHours = location.DockHours,
            PaymentTerms = location.PaymentTerms,
            BuyerStatus = location.BuyerStatus,
            SupplierStatus = location.SupplierStatus,
            IsActive = location.IsActive,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LocationFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var location = await Context.Locations.FindAsync(id);
        if (location is null)
        {
            return NotFound();
        }

        location.Location1 = model.Name;
        location.IsBuyer = GetIsBuyer(model.ClientType);
        location.ClientId = GetClientId(model);
        location.Country = model.Country;
        location.State = model.State;
        location.City = model.City;
        location.Address = model.Address;
        location.PinCode = model.PinCode;
        location.DockHours = model.DockHours;
        location.PaymentTerms = model.PaymentTerms;
        location.BuyerStatus = model.BuyerStatus;
        location.SupplierStatus = model.SupplierStatus;
        location.IsActive = model.IsActive;
        location.UpdatedOn = DateTime.UtcNow;
        location.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var location = await Context.Locations.FindAsync(id);
        if (location is null)
        {
            return NotFound();
        }

        location.IsActive = false;
        location.UpdatedOn = DateTime.UtcNow;
        location.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(LocationFormViewModel model)
    {
        model.ClientTypeOptions = GetClientTypeOptions();
        model.BuyerOptions = await Context.Buyers
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
            .ToListAsync();
        model.SupplierOptions = await Context.Suppliers
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
        model.CountryOptions = await Context.Countries
            .OrderBy(c => c.CountryName)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CountryName })
            .ToListAsync();
        model.StateOptions = await Context.States
            .OrderBy(s => s.StateName)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.StateName })
            .ToListAsync();
        model.PaymentTermOptions = await Context.PaymentTerms
            .OrderBy(p => p.Term)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Term })
            .ToListAsync();
        model.BuyerStatusOptions = await Context.BuyerStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();
        model.SupplierStatusOptions = await Context.SupplierStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();
    }

    private static IEnumerable<SelectListItem> GetClientTypeOptions()
    {
        yield return new SelectListItem { Value = BuyerClientType, Text = BuyerClientType };
        yield return new SelectListItem { Value = SupplierClientType, Text = SupplierClientType };
    }

    private static bool? GetIsBuyer(string? clientType) =>
        clientType == BuyerClientType ? true : clientType == SupplierClientType ? false : null;

    private static int? GetClientId(LocationFormViewModel model) =>
        model.ClientType == BuyerClientType ? model.BuyerClientId :
        model.ClientType == SupplierClientType ? model.SupplierClientId :
        null;

    private async Task<Data.Models.Location?> ValidateCopySourceAsync(LocationFormViewModel model)
    {
        if (!model.CopyLocationId.HasValue)
        {
            return null;
        }

        var source = await Context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == model.CopyLocationId.Value);
        if (source is null)
        {
            ModelState.AddModelError(nameof(model.CopyLocationId), "Choose a valid location to copy.");
            return null;
        }

        model.CopyLocationName = source.Location1;
        if (source.IsBuyer != GetIsBuyer(model.ClientType) || source.ClientId != GetClientId(model))
        {
            ModelState.AddModelError(nameof(model.CopyLocationId), "The copied location must belong to the same buyer or supplier.");
        }

        return source;
    }

    private async Task CopyLocationChildrenAsync(Data.Models.Location? source, Data.Models.Location target)
    {
        if (source is null)
        {
            return;
        }

        var userId = User.GetLegacyUserId();
        var now = DateTime.UtcNow;

        var contacts = await Context.Contacts
            .AsNoTracking()
            .Include(contact => contact.ContactNavigation)
            .Where(contact => contact.Location == source.Id && contact.IsActive && contact.IsDockContact != true)
            .ToListAsync();

        foreach (var contact in contacts)
        {
            target.Contacts.Add(new Data.Models.Contact
            {
                ContactNavigation = new Data.Models.ContactInformation
                {
                    FirstName = contact.ContactNavigation.FirstName,
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
                    IsActive = true,
                },
                ClientId = target.ClientId,
                IsBuyer = target.IsBuyer,
                IsPrimaryContact = contact.IsPrimaryContact,
                IsDockContact = contact.IsDockContact,
                IsActive = true,
                CreateOn = now,
                CreatedBy = userId,
            });
        }

        if (target.IsBuyer != true)
        {
            return;
        }

        var buyerProducts = await Context.BuyerProducts
            .AsNoTracking()
            .Include(product => product.BuyerProductPackagings)
            .Where(product => product.Location == source.Id && product.IsActive)
            .ToListAsync();

        foreach (var buyerProduct in buyerProducts)
        {
            target.BuyerProducts.Add(new Data.Models.BuyerProduct
            {
                Buyer = target.ClientId,
                Product = buyerProduct.Product,
                OtherProduct = buyerProduct.OtherProduct,
                IsActive = true,
                CreateOn = now,
                CreatedBy = userId,
                BuyerProductPackagings = buyerProduct.BuyerProductPackagings
                    .Select(packaging => new Data.Models.BuyerProductPackaging
                    {
                        Packaging = packaging.Packaging,
                        OtherPackaging = packaging.OtherPackaging,
                    })
                    .ToList(),
            });
        }
    }
}
