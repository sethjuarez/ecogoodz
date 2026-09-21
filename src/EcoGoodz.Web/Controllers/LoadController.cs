using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Load;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class LoadController : PagedListController<Data.Models.Load, LoadListItemViewModel>
{
    public LoadController(EcoGoodzDbContext context) : base(context)
    {
    }

    protected override IQueryable<Data.Models.Load> GetBaseQuery() =>
        Context.Loads
            .Include(l => l.BuyerNavigation)
            .Include(l => l.SupplierNavigation)
            .Include(l => l.LoadStatusNavigation);

    protected override IQueryable<Data.Models.Load> ApplySearch(IQueryable<Data.Models.Load> query, string searchTerm) =>
        query.Where(l =>
            (l.BuyerNavigation != null && l.BuyerNavigation.Name != null && l.BuyerNavigation.Name.Contains(searchTerm))
            || (l.SupplierNavigation != null && l.SupplierNavigation.Name != null && l.SupplierNavigation.Name.Contains(searchTerm))
            || (l.BuyerRef != null && l.BuyerRef.Contains(searchTerm))
            || (l.SupplierRef != null && l.SupplierRef.Contains(searchTerm))
            || (l.Container != null && l.Container.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<Data.Models.Load, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<Data.Models.Load, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["buyer"] = l => l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
            ["supplier"] = l => l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
            ["status"] = l => l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
            ["shipmentDate"] = l => l.ShipmentDate,
            ["active"] = l => l.IsActive,
        };

    protected override string DefaultSortColumn => "shipmentDate";

    protected override Expression<Func<Data.Models.Load, LoadListItemViewModel>> ProjectionExpression =>
        l => new LoadListItemViewModel
        {
            Id = l.Id,
            BuyerName = l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
            SupplierName = l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
            StatusName = l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
            ShipmentDate = l.ShipmentDate,
            IsActive = l.IsActive ?? false,
        };

    public async Task<IActionResult> Details(int id)
    {
        var load = await Context.Loads
            .Include(l => l.BuyerNavigation)
            .Include(l => l.SupplierNavigation)
            .Include(l => l.LoadStatusNavigation)
            .Where(l => l.Id == id)
            .Select(l => new LoadDetailsViewModel
            {
                Id = l.Id,
                BuyerId = l.Buyer,
                BuyerName = l.BuyerNavigation != null ? l.BuyerNavigation.Name : null,
                SupplierId = l.Supplier,
                SupplierName = l.SupplierNavigation != null ? l.SupplierNavigation.Name : null,
                StatusName = l.LoadStatusNavigation != null ? l.LoadStatusNavigation.Status : null,
                ShipmentDate = l.ShipmentDate,
                Container = l.Container,
                BuyerRef = l.BuyerRef,
                SupplierRef = l.SupplierRef,
                FreightCarrier = l.FreightCarrier,
                FreightInvoice = l.FreightInvoice,
                FreightAmountQuoted = l.FreightAmountQuoted,
                FreightAmountBilled = l.FreightAmountBilled,
                BuyerInvoice = l.BuyerInvoice,
                BuyerInvoiceAmount = l.BuyerInvoiceAmount,
                BuyerInvoiceDate = l.BuyerInvoiceDate,
                SupplierInvoice = l.SupplierInvoice,
                SupplierInvoiceAmount = l.SupplierInvoiceAmount,
                SupplierInvoiceDate = l.SupplierInvoiceDate,
                BuyerNotes = l.BuyerNotes,
                SupplierNotes = l.SupplierNotes,
                FreightNotes = l.FreightNotes,
                IsComplete = l.IsComplete ?? false,
                IsActive = l.IsActive ?? false,
                CreateOn = l.CreateOn,
                UpdatedOn = l.UpdatedOn,
                ProductLineCount = l.LoadProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (load is null)
        {
            return NotFound();
        }

        return View(load);
    }

    public async Task<IActionResult> Create()
    {
        var model = new LoadFormViewModel
        {
            BuyerOptions = await GetBuyerOptionsAsync(),
            SupplierOptions = await GetSupplierOptionsAsync(),
            StatusOptions = await GetStatusOptionsAsync(),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoadFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var load = new Data.Models.Load
        {
            Buyer = model.Buyer,
            Supplier = model.Supplier,
            LoadStatus = model.LoadStatus,
            ShipmentDate = model.ShipmentDate,
            Container = model.Container,
            BuyerRef = model.BuyerRef,
            SupplierRef = model.SupplierRef,
            FreightCarrier = model.FreightCarrier,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        Context.Loads.Add(load);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        var model = new LoadFormViewModel
        {
            Id = load.Id,
            Buyer = load.Buyer,
            Supplier = load.Supplier,
            LoadStatus = load.LoadStatus,
            ShipmentDate = load.ShipmentDate,
            Container = load.Container,
            BuyerRef = load.BuyerRef,
            SupplierRef = load.SupplierRef,
            FreightCarrier = load.FreightCarrier,
            IsActive = load.IsActive ?? false,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LoadFormViewModel model)
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

        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        load.Buyer = model.Buyer;
        load.Supplier = model.Supplier;
        load.LoadStatus = model.LoadStatus;
        load.ShipmentDate = model.ShipmentDate;
        load.Container = model.Container;
        load.BuyerRef = model.BuyerRef;
        load.SupplierRef = model.SupplierRef;
        load.FreightCarrier = model.FreightCarrier;
        load.IsActive = model.IsActive;
        load.UpdatedOn = DateTime.UtcNow;
        load.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Soft delete only, matching the legacy IsActive convention - loads are
    // referenced by gross profit projections and history throughout the schema.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var load = await Context.Loads.FindAsync(id);
        if (load is null)
        {
            return NotFound();
        }

        load.IsActive = false;
        load.UpdatedOn = DateTime.UtcNow;
        load.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(LoadFormViewModel model)
    {
        model.BuyerOptions = await GetBuyerOptionsAsync();
        model.SupplierOptions = await GetSupplierOptionsAsync();
        model.StatusOptions = await GetStatusOptionsAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetBuyerOptionsAsync() =>
        await Context.Buyers
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetSupplierOptionsAsync() =>
        await Context.Suppliers
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> GetStatusOptionsAsync() =>
        await Context.LoadStatuses
            .OrderBy(s => s.Status)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Status })
            .ToListAsync();
}
