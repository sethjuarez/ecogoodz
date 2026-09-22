using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.BuyerSupplier;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class BuyerSupplierController : PagedListController<Data.Models.BuyerSupplier, BuyerSupplierListItemViewModel>
{
    public BuyerSupplierController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.BuyerSupplier> GetBaseQuery() =>
        Context.BuyerSuppliers
            .Include(m => m.BuyerNavigation)
            .Include(m => m.SupplierNavigation)
            .Include(m => m.BuyerLocationNavigation)
            .Include(m => m.SupplierLocationNavigation)
            .Include(m => m.StatusNavigation);

    protected override IQueryable<Data.Models.BuyerSupplier> ApplySearch(IQueryable<Data.Models.BuyerSupplier> query, string searchTerm) =>
        query.Where(m =>
            (m.BuyerNavigation != null && m.BuyerNavigation.Name != null && m.BuyerNavigation.Name.Contains(searchTerm))
            || (m.SupplierNavigation != null && m.SupplierNavigation.Name != null && m.SupplierNavigation.Name.Contains(searchTerm))
            || (m.BuyerLocationNavigation != null && m.BuyerLocationNavigation.Location1 != null && m.BuyerLocationNavigation.Location1.Contains(searchTerm))
            || (m.SupplierLocationNavigation != null && m.SupplierLocationNavigation.Location1 != null && m.SupplierLocationNavigation.Location1.Contains(searchTerm))
            || (m.StatusNavigation != null && m.StatusNavigation.Status != null && m.StatusNavigation.Status.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.BuyerSupplier, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.BuyerSupplier, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["buyer"] = m => m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
            ["supplier"] = m => m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
            ["buyerLocation"] = m => m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
            ["supplierLocation"] = m => m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
            ["status"] = m => m.StatusNavigation != null ? m.StatusNavigation.Status : null,
            ["active"] = m => m.IsActive,
        };

    protected override string DefaultSortColumn => "buyer";

    protected override Expression<Func<Data.Models.BuyerSupplier, BuyerSupplierListItemViewModel>> ProjectionExpression =>
        m => new BuyerSupplierListItemViewModel
        {
            Id = m.Id,
            BuyerName = m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
            SupplierName = m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
            BuyerLocationName = m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
            SupplierLocationName = m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
            StatusName = m.StatusNavigation != null ? m.StatusNavigation.Status : null,
            IsActive = m.IsActive ?? false,
        };

    public async Task<IActionResult> Details(int id)
    {
        var match = await Context.BuyerSuppliers
            .Where(m => m.Id == id)
            .Select(m => new BuyerSupplierDetailsViewModel
            {
                Id = m.Id,
                BuyerName = m.BuyerNavigation != null ? m.BuyerNavigation.Name : null,
                SupplierName = m.SupplierNavigation != null ? m.SupplierNavigation.Name : null,
                BuyerLocationName = m.BuyerLocationNavigation != null ? m.BuyerLocationNavigation.Location1 : null,
                SupplierLocationName = m.SupplierLocationNavigation != null ? m.SupplierLocationNavigation.Location1 : null,
                StatusName = m.StatusNavigation != null ? m.StatusNavigation.Status : null,
                IsActive = m.IsActive ?? false,
                CreateOn = m.CreateOn,
                UpdatedOn = m.UpdatedOn,
                ProductCount = m.BuyerSupplierProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (match is null)
        {
            return NotFound();
        }

        var matchKeys = await Context.BuyerSuppliers
            .Where(m => m.Id == id)
            .Select(m => new { m.Buyer, m.Supplier, m.BuyerLocation, m.SupplierLocation })
            .FirstAsync();

        match.LoadCount = await Context.Loads.CountAsync(l =>
            l.Buyer == matchKeys.Buyer
            && l.Supplier == matchKeys.Supplier
            && l.BuyerLocation == matchKeys.BuyerLocation
            && l.SupplierLocation == matchKeys.SupplierLocation);

        return View(match);
    }

    public async Task<IActionResult> Create()
    {
        var model = new BuyerSupplierFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuyerSupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var match = new Data.Models.BuyerSupplier
        {
            Buyer = model.Buyer,
            Supplier = model.Supplier,
            BuyerLocation = model.BuyerLocation,
            SupplierLocation = model.SupplierLocation,
            Status = model.Status,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.BuyerSuppliers.Add(match);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        var model = new BuyerSupplierFormViewModel
        {
            Id = match.Id,
            Buyer = match.Buyer,
            Supplier = match.Supplier,
            BuyerLocation = match.BuyerLocation,
            SupplierLocation = match.SupplierLocation,
            Status = match.Status,
            IsActive = match.IsActive ?? false,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuyerSupplierFormViewModel model)
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

        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        match.Buyer = model.Buyer;
        match.Supplier = model.Supplier;
        match.BuyerLocation = model.BuyerLocation;
        match.SupplierLocation = model.SupplierLocation;
        match.Status = model.Status;
        match.IsActive = model.IsActive;
        match.UpdatedOn = DateTime.UtcNow;
        match.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var match = await Context.BuyerSuppliers.FindAsync(id);
        if (match is null)
        {
            return NotFound();
        }

        match.IsActive = false;
        match.UpdatedOn = DateTime.UtcNow;
        match.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(BuyerSupplierFormViewModel model)
    {
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
        model.BuyerLocationOptions = await Context.Locations
            .Where(l => l.IsActive && l.IsBuyer == true)
            .OrderBy(l => l.Location1)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1 })
            .ToListAsync();
        model.SupplierLocationOptions = await Context.Locations
            .Where(l => l.IsActive && l.IsBuyer == false)
            .OrderBy(l => l.Location1)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Location1 })
            .ToListAsync();
        model.StatusOptions = await Context.SupplierBuyerStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();
    }
}
