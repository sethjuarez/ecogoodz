using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Web.Extensions;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoGoodz.Web.Controllers.Shared;

/// <summary>
/// Base controller for the paged/sorted/searched Index action shared by every
/// large-list page (Buyer, Supplier, Product, ...). <typeparamref name="TRow"/>
/// is whatever queryable "row" a list is built from - usually the EF entity
/// itself, but it can be a join/projection type (e.g. ProductController's
/// self-join for parent name) when the raw entity alone isn't enough to
/// search/sort on. <typeparamref name="TListItem"/> is the view model the
/// Index view renders.
///
/// Concrete controllers only describe their query, search predicate, sort
/// columns, and row-&gt;view-model projection; paging/sorting/search plumbing
/// (query-string parsing, EF translation, PagedResult construction) lives here
/// exactly once. The EcoGoodzDbContext dependency is injected once in this
/// base constructor rather than repeated in every derived controller.
/// </summary>
[Authorize]
public abstract class PagedListController<TRow, TListItem> : Controller
{
    protected PagedListController(EcoGoodzDbContext context)
    {
        Context = context;
    }

    protected EcoGoodzDbContext Context { get; }

    /// <summary>The unfiltered, unsorted queryable for the list (Include navigations as needed).</summary>
    protected abstract IQueryable<TRow> GetBaseQuery();

    /// <summary>Applies a free-text search term across whatever columns matter for this list.</summary>
    protected abstract IQueryable<TRow> ApplySearch(IQueryable<TRow> query, string searchTerm);

    /// <summary>Query-string column key -&gt; EF sort expression, for every column users can click to sort.</summary>
    protected abstract IReadOnlyDictionary<string, Expression<Func<TRow, object?>>> SortColumns { get; }

    /// <summary>Sort column key used when no (or an unrecognized) sort is requested.</summary>
    protected abstract string DefaultSortColumn { get; }

    /// <summary>Projects a sorted row into the view model the Index view renders.</summary>
    protected abstract Expression<Func<TRow, TListItem>> ProjectionExpression { get; }

    public virtual async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = PageInfo.DefaultPageSize)
    {
        var query = GetBaseQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        var sorted = query.ApplySort(sort, desc, SortColumns, DefaultSortColumn, out var resolvedSort);
        var projected = sorted.Select(ProjectionExpression);
        var result = await projected.ToPagedResultAsync(page, pageSize, search, resolvedSort, desc);

        return View("Index", result);
    }
}
