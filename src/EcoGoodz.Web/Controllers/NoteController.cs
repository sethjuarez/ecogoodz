using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Note;
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

    public async Task<IActionResult> Index(string? scope, int? id)
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

        var notes = await query
            .OrderByDescending(n => n.UpdatedOn ?? n.CreateOn)
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

        return View(notes);
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
        model.BuyerLocationOptions = await _context.Locations
            .Where(l => l.IsBuyer == true && l.IsActive)
            .OrderBy(l => l.Location1)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1 })
            .ToListAsync();
        model.SupplierLocationOptions = await _context.Locations
            .Where(l => l.IsBuyer == false && l.IsActive)
            .OrderBy(l => l.Location1)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1 })
            .ToListAsync();
        model.ProductOptions = await _context.SupplierProducts
            .Include(sp => sp.SupplierNavigation)
            .Include(sp => sp.ProductNavigation)
            .Where(sp => sp.IsActive == true)
            .OrderBy(sp => sp.SupplierNavigation!.Name)
            .ThenBy(sp => sp.ProductNavigation!.Name)
            .Select(sp => new SelectListItem
            {
                Value = sp.Id.ToString(),
                Text = (sp.SupplierNavigation != null ? sp.SupplierNavigation.Name : "Supplier") + " - " + (sp.ProductNavigation != null ? sp.ProductNavigation.Name : "Product"),
            })
            .ToListAsync();
    }
}
