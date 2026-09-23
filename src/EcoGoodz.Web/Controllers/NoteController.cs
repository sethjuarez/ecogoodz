using System.Globalization;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Note;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class NoteController : Controller
{
    private const string BuyerLocationScope = "BuyerLocation";
    private const string SupplierLocationScope = "SupplierLocation";
    private const string ProductScope = "Product";
    private readonly EcoGoodzDbContext _context;

    public NoteController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? scope, int? id, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        var query = _context.Notes
            .Where(n => n.IsActive == true)
            .Include(n => n.BuyerLocationNavigation)
            .Include(n => n.SupplierLocationNavigation)
            .Include(n => n.ProductNavigation)
                .ThenInclude(p => p!.ProductNavigation)
            .Include(n => n.CreatedByNavigation)
            .AsQueryable();

        query = scope switch
        {
            BuyerLocationScope when id.HasValue => query.Where(n => n.BuyerLocation == id),
            SupplierLocationScope when id.HasValue => query.Where(n => n.SupplierLocation == id),
            ProductScope when id.HasValue => query.Where(n => n.Product == id && n.IsProduct),
            _ => query,
        };

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? PageInfo.DefaultPageSize : pageSize;
        var totalCount = await query.CountAsync();
        var scopeParameters = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(scope))
        {
            scopeParameters["scope"] = scope;
        }

        if (id.HasValue)
        {
            scopeParameters["id"] = id.Value.ToString(CultureInfo.InvariantCulture);
        }

        var notes = await query
            .OrderByDescending(n => n.UpdatedOn ?? n.CreateOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteListItemViewModel
            {
                Id = n.Id,
                LocationId = n.BuyerLocation ?? n.SupplierLocation,
                ProductId = n.Product,
                Scope = n.IsProduct ? "Product" : n.BuyerLocation.HasValue ? "Buyer location" : "Supplier location",
                LocationName = n.BuyerLocationNavigation != null
                    ? n.BuyerLocationNavigation.Location1
                    : n.SupplierLocationNavigation != null ? n.SupplierLocationNavigation.Location1 : null,
                ProductName = n.ProductNavigation != null && n.ProductNavigation.ProductNavigation != null
                    ? n.ProductNavigation.ProductNavigation.Name
                    : null,
                Notes = n.Notes ?? string.Empty,
                Date = n.UpdatedOn ?? n.CreateOn,
                CreatedByName = n.CreatedByNavigation != null
                    ? n.CreatedByNavigation.FirstName + " " + n.CreatedByNavigation.LastName
                    : null,
                IsActive = n.IsActive == true,
            })
            .ToListAsync();

        return View(new PagedResult<NoteListItemViewModel>
        {
            Items = notes,
            Page = new PageInfo
            {
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                AdditionalQueryParameters = scopeParameters,
            },
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var note = await _context.Notes
            .Where(n => n.Id == id)
            .Include(n => n.BuyerLocationNavigation)
            .Include(n => n.SupplierLocationNavigation)
            .Include(n => n.ProductNavigation)
                .ThenInclude(p => p!.ProductNavigation)
            .Include(n => n.CreatedByNavigation)
            .Select(n => new NoteListItemViewModel
            {
                Id = n.Id,
                LocationId = n.BuyerLocation ?? n.SupplierLocation,
                ProductId = n.Product,
                Scope = n.IsProduct ? "Product" : n.BuyerLocation.HasValue ? "Buyer location" : "Supplier location",
                LocationName = n.BuyerLocationNavigation != null
                    ? n.BuyerLocationNavigation.Location1
                    : n.SupplierLocationNavigation != null ? n.SupplierLocationNavigation.Location1 : null,
                ProductName = n.ProductNavigation != null && n.ProductNavigation.ProductNavigation != null
                    ? n.ProductNavigation.ProductNavigation.Name
                    : null,
                Notes = n.Notes ?? string.Empty,
                Date = n.UpdatedOn ?? n.CreateOn,
                CreatedByName = n.CreatedByNavigation != null
                    ? n.CreatedByNavigation.FirstName + " " + n.CreatedByNavigation.LastName
                    : null,
                IsActive = n.IsActive == true,
            })
            .FirstOrDefaultAsync();

        return note is null ? NotFound() : View(note);
    }

    public async Task<IActionResult> Create(string? scope, int? id)
    {
        var model = new NoteFormViewModel { Scope = NormalizeScope(scope) };
        ApplyScopeId(model, id);
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NoteFormViewModel model)
    {
        ValidateScope(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var note = new Note
        {
            Notes = model.Notes,
            BuyerLocation = model.Scope == BuyerLocationScope ? model.BuyerLocation : null,
            SupplierLocation = model.Scope == SupplierLocationScope ? model.SupplierLocation : null,
            Product = model.Scope == ProductScope ? model.Product : null,
            IsProduct = model.Scope == ProductScope,
            IsBuyer = model.Scope == BuyerLocationScope ? true : model.Scope == SupplierLocationScope ? false : null,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { scope = model.Scope, id = GetScopedId(model) });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note is null)
        {
            return NotFound();
        }

        var model = new NoteFormViewModel
        {
            Id = note.Id,
            Scope = note.IsProduct ? ProductScope : note.BuyerLocation.HasValue ? BuyerLocationScope : SupplierLocationScope,
            BuyerLocation = note.BuyerLocation,
            SupplierLocation = note.SupplierLocation,
            Product = note.Product,
            Notes = note.Notes ?? string.Empty,
            IsActive = note.IsActive == true,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NoteFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        ValidateScope(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var note = await _context.Notes.FindAsync(id);
        if (note is null)
        {
            return NotFound();
        }

        note.Notes = model.Notes;
        note.BuyerLocation = model.Scope == BuyerLocationScope ? model.BuyerLocation : null;
        note.SupplierLocation = model.Scope == SupplierLocationScope ? model.SupplierLocation : null;
        note.Product = model.Scope == ProductScope ? model.Product : null;
        note.IsProduct = model.Scope == ProductScope;
        note.IsBuyer = model.Scope == BuyerLocationScope ? true : model.Scope == SupplierLocationScope ? false : null;
        note.IsActive = model.IsActive;
        note.UpdatedOn = DateTime.UtcNow;
        note.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { scope = model.Scope, id = GetScopedId(model) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note is null)
        {
            return NotFound();
        }

        note.IsActive = false;
        note.UpdatedOn = DateTime.UtcNow;
        note.UpdatedBy = User.GetLegacyUserId();
        await _context.SaveChangesAsync();

        var scope = note.IsProduct ? ProductScope : note.BuyerLocation.HasValue ? BuyerLocationScope : SupplierLocationScope;
        var scopedId = note.IsProduct ? note.Product : note.BuyerLocation ?? note.SupplierLocation;
        return RedirectToAction(nameof(Index), new { scope, id = scopedId });
    }

    public async Task<IActionResult> SearchBuyerLocations(string? q)
    {
        var query = _context.Locations.AsNoTracking().Where(location => location.IsBuyer == true && location.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(location =>
                (location.Location1 != null && location.Location1.Contains(q))
                || (location.City != null && location.City.Contains(q))
                || (location.Address != null && location.Address.Contains(q)));
        }

        return Json(await query
            .OrderBy(location => location.Location1)
            .Take(50)
            .Select(location => new SelectOption(location.Id.ToString(), location.Location1 ?? "(unnamed buyer location)"))
            .ToListAsync());
    }

    public async Task<IActionResult> SearchSupplierLocations(string? q)
    {
        var query = _context.Locations.AsNoTracking().Where(location => location.IsBuyer == false && location.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(location =>
                (location.Location1 != null && location.Location1.Contains(q))
                || (location.City != null && location.City.Contains(q))
                || (location.Address != null && location.Address.Contains(q)));
        }

        return Json(await query
            .OrderBy(location => location.Location1)
            .Take(50)
            .Select(location => new SelectOption(location.Id.ToString(), location.Location1 ?? "(unnamed supplier location)"))
            .ToListAsync());
    }

    public async Task<IActionResult> SearchProducts(string? q)
    {
        var query = _context.SupplierProducts
            .AsNoTracking()
            .Where(product => product.IsActive == true);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(product =>
                (product.SupplierNavigation != null && product.SupplierNavigation.Name != null && product.SupplierNavigation.Name.Contains(q))
                || (product.ProductNavigation != null && product.ProductNavigation.Name != null && product.ProductNavigation.Name.Contains(q))
                || (product.OtherPackaging != null && product.OtherPackaging.Contains(q)));
        }

        return Json(await query
            .OrderBy(product => product.SupplierNavigation!.Name)
            .ThenBy(product => product.ProductNavigation!.Name)
            .Take(50)
            .Select(product => new SelectOption(
                product.Id.ToString(),
                (product.SupplierNavigation != null ? product.SupplierNavigation.Name : "Supplier")
                    + " - "
                    + (product.ProductNavigation != null ? product.ProductNavigation.Name : "Product")))
            .ToListAsync());
    }

    private static string NormalizeScope(string? scope) =>
        scope is SupplierLocationScope or ProductScope ? scope : BuyerLocationScope;

    private static void ApplyScopeId(NoteFormViewModel model, int? id)
    {
        if (!id.HasValue)
        {
            return;
        }

        if (model.Scope == BuyerLocationScope)
        {
            model.BuyerLocation = id;
        }
        else if (model.Scope == SupplierLocationScope)
        {
            model.SupplierLocation = id;
        }
        else
        {
            model.Product = id;
        }
    }

    private void ValidateScope(NoteFormViewModel model)
    {
        model.Scope = NormalizeScope(model.Scope);
        if (model.Scope == BuyerLocationScope && !model.BuyerLocation.HasValue)
        {
            ModelState.AddModelError(nameof(model.BuyerLocation), "Choose a buyer location.");
        }
        else if (model.Scope == SupplierLocationScope && !model.SupplierLocation.HasValue)
        {
            ModelState.AddModelError(nameof(model.SupplierLocation), "Choose a supplier location.");
        }
        else if (model.Scope == ProductScope && !model.Product.HasValue)
        {
            ModelState.AddModelError(nameof(model.Product), "Choose a supplier product.");
        }
    }

    private static int? GetScopedId(NoteFormViewModel model) =>
        model.Scope == BuyerLocationScope ? model.BuyerLocation :
        model.Scope == SupplierLocationScope ? model.SupplierLocation :
        model.Product;

    private async Task PopulateOptionsAsync(NoteFormViewModel model)
    {
        model.ScopeOptions =
        [
            new SelectListItem { Value = BuyerLocationScope, Text = "Buyer location" },
            new SelectListItem { Value = SupplierLocationScope, Text = "Supplier location" },
            new SelectListItem { Value = ProductScope, Text = "Supplier product" },
        ];
        model.BuyerLocationOptions = await GetSelectedLocationOptionsAsync(model.BuyerLocation);
        model.SupplierLocationOptions = await GetSelectedLocationOptionsAsync(model.SupplierLocation);
        model.ProductOptions = await GetSelectedProductOptionsAsync(model.Product);
    }

    private async Task<List<SelectListItem>> GetSelectedLocationOptionsAsync(int? selectedId) =>
        selectedId is null
            ? []
            : await _context.Locations
                .AsNoTracking()
                .Where(location => location.Id == selectedId)
                .Select(location => new SelectListItem
                {
                    Value = location.Id.ToString(),
                    Text = location.Location1 ?? "(unnamed location)",
                    Selected = true,
                })
                .ToListAsync();

    private async Task<List<SelectListItem>> GetSelectedProductOptionsAsync(int? selectedId) =>
        selectedId is null
            ? []
            : await _context.SupplierProducts
                .AsNoTracking()
                .Where(product => product.Id == selectedId)
                .Select(product => new SelectListItem
                {
                    Value = product.Id.ToString(),
                    Text = (product.SupplierNavigation != null ? product.SupplierNavigation.Name : "Supplier")
                        + " - "
                        + (product.ProductNavigation != null ? product.ProductNavigation.Name : "Product"),
                    Selected = true,
                })
                .ToListAsync();

    private sealed record SelectOption(string Value, string Text);
}
