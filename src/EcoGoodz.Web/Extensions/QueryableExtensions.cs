using System.Linq.Expressions;
using EcoGoodz.Web.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applies ordering for one of a controller's allowed sort columns. Pass a
    /// dictionary mapping the query-string column key (e.g. "name") to the
    /// property selector EF should order by; unknown/omitted columns fall back
    /// to <paramref name="defaultColumn"/>. The resolved column is handed back
    /// via <paramref name="resolvedColumn"/> so it can be stamped onto the
    /// resulting <see cref="PageInfo"/> for sort-arrow highlighting and link
    /// building in the shared list views.
    /// </summary>
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? requestedColumn,
        bool descending,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> sortColumns,
        string defaultColumn,
        out string resolvedColumn)
    {
        resolvedColumn = requestedColumn is not null && sortColumns.ContainsKey(requestedColumn)
            ? requestedColumn
            : defaultColumn;

        var selector = sortColumns[resolvedColumn];
        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    /// <summary>
    /// Applies skip/take paging to an already-ordered, already-filtered query
    /// and materializes both the page of items and the total count in two
    /// queries (count against the pre-paged query, items against the paged
    /// one). Centralized here so every Index action pages the same way.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortColumn = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 20 : pageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = new PageInfo
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = searchTerm,
                SortColumn = sortColumn,
                SortDescending = sortDescending,
            },
        };
    }
}
