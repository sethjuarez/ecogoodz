using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

public class ProductController : PagedListController<ProductController.ProductRow, ProductListItemViewModel>
{
    public ProductController(EcoGoodzDbContext context) : base(context)
    {
    }

    // Product.IdParent is a plain int FK with no EF navigation property (see
    // Product.cs), so parent name requires an explicit self-join rather than
    // an Include - projected into ProductRow so search/sort can see both.
    protected override IQueryable<ProductRow> GetBaseQuery() =>
        from p in Context.Products
        join parent in Context.Products on p.IdParent equals parent.Id into parentJoin
        from parent in parentJoin.DefaultIfEmpty()
        select new ProductRow { Product = p, ParentName = parent.Name };

    protected override IQueryable<ProductRow> ApplySearch(IQueryable<ProductRow> query, string searchTerm) =>
        query.Where(r =>
            (r.Product.Name != null && r.Product.Name.Contains(searchTerm))
            || (r.ParentName != null && r.ParentName.Contains(searchTerm)));

    protected override IReadOnlyDictionary<string, Expression<Func<ProductRow, object?>>> SortColumns { get; } =
        new Dictionary<string, Expression<Func<ProductRow, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = r => EF.Property<string>(r.Product, "NameSort"),
            ["parent"] = r => r.ParentName,
            ["active"] = r => r.Product.IsActive,
        };

    protected override string DefaultSortColumn => "name";

    protected override Expression<Func<ProductRow, ProductListItemViewModel>> ProjectionExpression =>
        r => new ProductListItemViewModel
        {
            Id = r.Product.Id,
            Name = r.Product.Name ?? string.Empty,
            ParentName = r.ParentName,
            IsActive = r.Product.IsActive ?? false,
        };

    public sealed class ProductRow
    {
        public required Data.Models.Product Product { get; init; }
        public string? ParentName { get; init; }
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await Context.Products
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

        var parentId = await Context.Products.Where(p => p.Id == id).Select(p => p.IdParent).FirstOrDefaultAsync();
        if (parentId.HasValue)
        {
            product.ParentName = await Context.Products
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

        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await Context.Products.FindAsync(id);
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

        var product = await Context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = model.Name;
        product.IdParent = model.IdParent;
        product.IsActive = model.IsActive;
        product.UpdatedOn = DateTime.UtcNow;
        product.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var product = await Context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        product.UpdatedOn = DateTime.UtcNow;
        product.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetParentOptionsAsync(int? excludeId)
    {
        return await Context.Products
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
