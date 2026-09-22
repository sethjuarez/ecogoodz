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
        select new ProductRow { Product = p, ParentId = parent != null ? parent.Id : null, ParentName = parent != null ? parent.Name : null };

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
            ParentId = r.ParentId,
            ParentName = r.ParentName,
            IsActive = r.Product.IsActive ?? false,
        };

    public sealed class ProductRow
    {
        public required Data.Models.Product Product { get; init; }
        public int? ParentId { get; init; }
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

    public async Task<IActionResult> Markup(int id)
    {
        var model = await BuildMarkupModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Markup(ProductMarkupViewModel model)
    {
        var product = await Context.Products.FindAsync(model.ProductId);
        if (product is null)
        {
            return NotFound();
        }

        var duplicatePostedColorIds = model.MarkupColors
            .GroupBy(row => row.MarkUpColorId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicatePostedColorIds.Count != 0)
        {
            ModelState.AddModelError(string.Empty, "Each markup color can only be submitted once.");
        }

        ValidateMarkupRows(model.MarkupColors, nameof(model.MarkupColors));
        var allowedColorIds = await Context.MarkUpColors.Select(color => color.Id).ToListAsync();
        var allowedColorIdSet = allowedColorIds.ToHashSet();
        foreach (var row in model.MarkupColors)
        {
            if (!allowedColorIdSet.Contains(row.MarkUpColorId))
            {
                ModelState.AddModelError(string.Empty, "One or more markup colors is no longer available.");
            }
        }

        var postedColorIdSet = model.MarkupColors.Select(row => row.MarkUpColorId).ToHashSet();
        if (!allowedColorIdSet.SetEquals(postedColorIdSet))
        {
            ModelState.AddModelError(string.Empty, "The markup color list is out of date. Reload the page and try again.");
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildMarkupModelAsync(model.ProductId);
            if (rebuilt is null)
            {
                return NotFound();
            }

            RestoreMarkupRows(rebuilt.MarkupColors, model.MarkupColors);
            return View(rebuilt);
        }

        var existingRows = await Context.ProductMarkUpColors
            .Where(row => row.Product == model.ProductId)
            .ToListAsync();
        var existingByColor = existingRows
            .Where(row => row.MarkUpColor.HasValue)
            .GroupBy(row => row.MarkUpColor!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var row in model.MarkupColors)
        {
            if (existingByColor.TryGetValue(row.MarkUpColorId, out var existingRowsForColor))
            {
                foreach (var existing in existingRowsForColor)
                {
                    existing.MinRate = row.MinRate;
                    existing.MaxRate = row.MaxRate;
                }
            }
            else
            {
                var created = new Data.Models.ProductMarkUpColor
                {
                    Product = model.ProductId,
                    MarkUpColor = row.MarkUpColorId,
                    MinRate = row.MinRate,
                    MaxRate = row.MaxRate,
                };
                Context.ProductMarkUpColors.Add(created);
                existingByColor[row.MarkUpColorId] = [created];
            }
        }

        product.UpdatedOn = DateTime.UtcNow;
        product.UpdatedBy = User.GetLegacyUserId();

        await Context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = model.ProductId });
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

    private async Task<ProductMarkupViewModel?> BuildMarkupModelAsync(int productId)
    {
        var product = await Context.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new { product.Id, product.Name })
            .FirstOrDefaultAsync();
        if (product is null)
        {
            return null;
        }

        var existingRows = await Context.ProductMarkUpColors
            .AsNoTracking()
            .Where(row => row.Product == productId)
            .ToListAsync();
        var existingByColor = existingRows
            .Where(row => row.MarkUpColor.HasValue)
            .GroupBy(row => row.MarkUpColor!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.Id).First());

        var colors = await Context.MarkUpColors
            .AsNoTracking()
            .OrderBy(color => color.Id)
            .Select(color => new { color.Id, color.Color, color.ColorCode })
            .ToListAsync();

        return new ProductMarkupViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name ?? string.Empty,
            MarkupColors = colors.Select(color =>
            {
                existingByColor.TryGetValue(color.Id, out var existing);
                return new ProductMarkupColorLineViewModel
                {
                    Id = existing?.Id,
                    MarkUpColorId = color.Id,
                    Color = color.Color ?? string.Empty,
                    ColorCode = color.ColorCode,
                    MinRate = existing?.MinRate,
                    MaxRate = existing?.MaxRate,
                };
            }).ToList(),
        };
    }

    private void ValidateMarkupRows(IReadOnlyList<ProductMarkupColorLineViewModel> rows, string prefix)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.MinRate.HasValue != row.MaxRate.HasValue)
            {
                ModelState.AddModelError($"{prefix}[{index}].MinRate", $"Enter both minimum and maximum margins for {row.Color}, or leave both blank.");
                ModelState.AddModelError($"{prefix}[{index}].MaxRate", $"Enter both minimum and maximum margins for {row.Color}, or leave both blank.");
            }

            if (row.MinRate.HasValue && row.MaxRate.HasValue && row.MinRate > row.MaxRate)
            {
                ModelState.AddModelError($"{prefix}[{index}].MinRate", $"Minimum margin must be less than or equal to maximum margin for {row.Color}.");
            }
        }

        var configuredRows = rows
            .Select((row, index) => new { row, index })
            .Where(item => item.row.MinRate.HasValue && item.row.MaxRate.HasValue)
            .ToList();
        foreach (var current in configuredRows)
        {
            foreach (var other in configuredRows.Where(item => item.index > current.index))
            {
                if (current.row.MinRate <= other.row.MaxRate && other.row.MinRate <= current.row.MaxRate)
                {
                    ModelState.AddModelError(string.Empty, $"Margin parameters are conflicted for colors {current.row.Color} and {other.row.Color}.");
                }
            }
        }
    }

    private static void RestoreMarkupRows(List<ProductMarkupColorLineViewModel> rebuiltRows, List<ProductMarkupColorLineViewModel> postedRows)
    {
        var postedByColor = postedRows
            .GroupBy(row => row.MarkUpColorId)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var row in rebuiltRows)
        {
            if (!postedByColor.TryGetValue(row.MarkUpColorId, out var posted))
            {
                continue;
            }

            row.Id = posted.Id;
            row.MinRate = posted.MinRate;
            row.MaxRate = posted.MaxRate;
        }
    }
}
