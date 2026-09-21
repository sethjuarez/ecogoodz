using EcoGoodz.Web.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Extensions;

public static class QueryableExtensions
{
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
            },
        };
    }
}
