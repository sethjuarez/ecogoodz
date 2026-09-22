using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class CommunicationController : Controller
{
    private const string BuyerClientType = "Buyer";
    private const string SupplierClientType = "Supplier";
    private readonly EcoGoodzDbContext _context;

    public CommunicationController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? clientType, int? clientId)
    {
        var isBuyer = clientType == SupplierClientType ? false : true;
        var query = CommunicationRows();

        if (clientId.HasValue)
        {
            query = query.Where(c => c.Communication.ClientId == clientId && (c.Communication.IsBuyer ?? true) == isBuyer);
        }

        var communications = await query
            .OrderByDescending(c => c.Communication.Date ?? c.Communication.CreateOn)
            .Select(c => new CommunicationListItemViewModel
            {
                Id = c.Communication.Id,
                ClientId = c.Communication.ClientId,
                IsBuyer = c.Communication.IsBuyer == true,
                ClientName = c.Communication.IsBuyer == true ? c.BuyerName : c.SupplierName,
                TypeName = c.Communication.CommunicationTypeNavigation != null
                    ? c.Communication.CommunicationTypeNavigation.Type ?? string.Empty
                    : c.Communication.OtherType ?? "Other",
                Note = c.Communication.Note,
                Date = c.Communication.Date ?? c.Communication.CreateOn,
                CreatedByName = c.Communication.CreatedByNavigation != null
                    ? c.Communication.CreatedByNavigation.FirstName + " " + c.Communication.CreatedByNavigation.LastName
                    : null,
            })
            .ToListAsync();

        return View(communications);
    }

    public async Task<IActionResult> Details(int id)
    {
        var communication = await CommunicationRows()
            .Where(c => c.Communication.Id == id)
            .Select(c => new CommunicationListItemViewModel
            {
                Id = c.Communication.Id,
                ClientId = c.Communication.ClientId,
                IsBuyer = c.Communication.IsBuyer == true,
                ClientName = c.Communication.IsBuyer == true ? c.BuyerName : c.SupplierName,
                TypeName = c.Communication.CommunicationTypeNavigation != null
                    ? c.Communication.CommunicationTypeNavigation.Type ?? string.Empty
                    : c.Communication.OtherType ?? "Other",
                Note = c.Communication.Note,
                Date = c.Communication.Date ?? c.Communication.CreateOn,
                CreatedByName = c.Communication.CreatedByNavigation != null
                    ? c.Communication.CreatedByNavigation.FirstName + " " + c.Communication.CreatedByNavigation.LastName
                    : null,
            })
            .FirstOrDefaultAsync();

        return communication is null ? NotFound() : View(communication);
    }

    public async Task<IActionResult> Create(string? clientType, int? clientId)
    {
        var model = new CommunicationFormViewModel { ClientType = clientType == SupplierClientType ? SupplierClientType : BuyerClientType };
        if (clientId.HasValue)
        {
            if (model.ClientType == BuyerClientType)
            {
                model.BuyerId = clientId;
            }
            else
            {
                model.SupplierId = clientId;
            }
        }

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommunicationFormViewModel model)
    {
        ValidateClientAndType(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var isBuyer = model.ClientType == BuyerClientType;
        var communication = new Communication
        {
            ClientId = isBuyer ? model.BuyerId : model.SupplierId,
            IsBuyer = isBuyer,
            CommunicationType = model.CommunicationType,
            OtherType = model.CommunicationType.HasValue ? null : model.OtherType,
            Note = model.Note,
            Date = model.Date ?? DateTime.UtcNow,
            IsActive = true,
            TrackingId = User.GetLegacyUserId() ?? 0,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { clientType = model.ClientType, clientId = communication.ClientId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var communication = await _context.Communications.FindAsync(id);
        if (communication is null)
        {
            return NotFound();
        }

        var model = new CommunicationFormViewModel
        {
            Id = communication.Id,
            ClientType = communication.IsBuyer == false ? SupplierClientType : BuyerClientType,
            BuyerId = communication.IsBuyer == false ? null : communication.ClientId,
            SupplierId = communication.IsBuyer == false ? communication.ClientId : null,
            CommunicationType = communication.CommunicationType,
            OtherType = communication.OtherType,
            Note = communication.Note ?? string.Empty,
            Date = communication.Date,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommunicationFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        ValidateClientAndType(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var communication = await _context.Communications.FindAsync(id);
        if (communication is null)
        {
            return NotFound();
        }

        var isBuyer = model.ClientType == BuyerClientType;
        communication.ClientId = isBuyer ? model.BuyerId : model.SupplierId;
        communication.IsBuyer = isBuyer;
        communication.CommunicationType = model.CommunicationType;
        communication.OtherType = model.CommunicationType.HasValue ? null : model.OtherType;
        communication.Note = model.Note;
        communication.Date = model.Date ?? DateTime.UtcNow;
        communication.UpdatedOn = DateTime.UtcNow;
        communication.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { clientType = model.ClientType, clientId = communication.ClientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var communication = await _context.Communications.FindAsync(id);
        if (communication is null)
        {
            return NotFound();
        }

        communication.IsActive = false;
        communication.UpdatedOn = DateTime.UtcNow;
        communication.UpdatedBy = User.GetLegacyUserId();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new
        {
            clientType = communication.IsBuyer == false ? SupplierClientType : BuyerClientType,
            clientId = communication.ClientId,
        });
    }

    private void ValidateClientAndType(CommunicationFormViewModel model)
    {
        if (model.ClientType == BuyerClientType && !model.BuyerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BuyerId), "Choose a buyer.");
        }
        else if (model.ClientType == SupplierClientType && !model.SupplierId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Choose a supplier.");
        }

        if (!model.CommunicationType.HasValue && string.IsNullOrWhiteSpace(model.OtherType))
        {
            ModelState.AddModelError(nameof(model.OtherType), "Choose a type or enter an other type.");
        }
    }

    private async Task PopulateOptionsAsync(CommunicationFormViewModel model)
    {
        model.ClientTypeOptions =
        [
            new SelectListItem { Value = BuyerClientType, Text = BuyerClientType },
            new SelectListItem { Value = SupplierClientType, Text = SupplierClientType },
        ];
        model.BuyerOptions = await _context.Buyers
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
            .ToListAsync();
        model.SupplierOptions = await _context.Suppliers
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
        model.CommunicationTypeOptions = await _context.CommunicationTypes
            .OrderBy(t => t.Type)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Type })
            .ToListAsync();
    }

    private IQueryable<CommunicationRow> CommunicationRows() =>
        from communication in _context.Communications
            .Where(c => c.IsActive)
            .Include(c => c.CommunicationTypeNavigation)
            .Include(c => c.CreatedByNavigation)
        join buyer in _context.Buyers on communication.ClientId equals buyer.Id into buyerJoin
        from buyer in buyerJoin.DefaultIfEmpty()
        join supplier in _context.Suppliers on communication.ClientId equals supplier.Id into supplierJoin
        from supplier in supplierJoin.DefaultIfEmpty()
        select new CommunicationRow
        {
            Communication = communication,
            BuyerName = communication.IsBuyer == true ? buyer.Name : null,
            SupplierName = communication.IsBuyer == false ? supplier.Name : null,
        };

    private sealed class CommunicationRow
    {
        public required Communication Communication { get; init; }
        public string? BuyerName { get; init; }
        public string? SupplierName { get; init; }
    }
}
