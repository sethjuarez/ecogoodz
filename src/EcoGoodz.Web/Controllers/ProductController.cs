using EcoGoodz.Data;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public ProductController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
        var namesById = products.ToDictionary(p => p.Id, p => p.Name ?? string.Empty);

        var model = products.Select(p => new ProductListItemViewModel
        {
            Id = p.Id,
            Name = p.Name ?? string.Empty,
            ParentName = p.IdParent.HasValue && namesById.TryGetValue(p.IdParent.Value, out var parentName)
                ? parentName
                : null,
            IsActive = p.IsActive ?? false,
        });

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailsViewModel
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                IsActive = p.IsActive ?? false,
                CreateOn = p.CreateOn,
                UpdatedOn = p.UpdatedOn,
                BuyerCount = p.BuyerProducts.Count,
                SupplierCount = p.SupplierProducts.Count,
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        var parentId = await _context.Products.Where(p => p.Id == id).Select(p => p.IdParent).FirstOrDefaultAsync();
        if (parentId.HasValue)
        {
            product.ParentName = await _context.Products
                .Where(p => p.Id == parentId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        return View(product);
    }

    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel { ParentOptions = await GetParentOptionsAsync(null) };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ParentOptions = await GetParentOptionsAsync(null);
            return View(model);
        }

        var product = new Data.Models.Product
        {
            Name = model.Name,
            IdParent = model.IdParent,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name ?? string.Empty,
            IdParent = product.IdParent,
            IsActive = product.IsActive ?? false,
            ParentOptions = await GetParentOptionsAsync(id),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (model.IdParent == id)
        {
            ModelState.AddModelError(nameof(model.IdParent), "A product cannot be its own parent.");
        }

        if (!ModelState.IsValid)
        {
            model.ParentOptions = await GetParentOptionsAsync(id);
            return View(model);
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = model.Name;
        product.IdParent = model.IdParent;
        product.IsActive = model.IsActive;
        product.UpdatedOn = DateTime.UtcNow;
        product.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        product.UpdatedOn = DateTime.UtcNow;
        product.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetParentOptionsAsync(int? excludeId)
    {
        return await _context.Products
            .Where(p => excludeId == null || p.Id != excludeId)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name,
            })
            .ToListAsync();
    }
}
