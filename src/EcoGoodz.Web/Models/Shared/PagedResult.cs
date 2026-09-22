namespace EcoGoodz.Web.Models.Shared;

/// <summary>
/// Paging/search metadata for a list page, independent of the item type so a
/// single <c>_Pagination.cshtml</c>/<c>_ListSearchForm.cshtml</c> partial pair
/// can render it for any controller's Index view.
/// </summary>
public sealed class PageInfo
{
    /// <summary>
    /// The default page size used across every list controller/view when the
    /// caller doesn't specify one. Centralized here so it only needs to be
    /// updated in one place - it's compared against in the controller action
    /// signatures, <c>Html.SortableHeader</c>, <c>_ListSearchBox</c>, and
    /// <c>_ListPagination</c> to decide whether to include a <c>pageSize</c>
    /// query-string parameter.
    /// </summary>
    public const int DefaultPageSize = 20;

    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public string? SearchTerm { get; init; }

    /// <summary>
    /// The column actually applied for ordering (already resolved against the
    /// controller's allowed-column map - see <c>QueryableExtensions.ApplySort</c>).
    /// Never null once a query has gone through <c>ApplySort</c>, so views can
    /// safely compare against it to highlight the active sort column.
    /// </summary>
    public string? SortColumn { get; init; }
    public bool SortDescending { get; init; }
    public IReadOnlyDictionary<string, string?> AdditionalQueryParameters { get; init; } =
        new Dictionary<string, string?>();

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// A single page of <typeparamref name="T"/> plus the <see cref="PageInfo"/>
/// needed to render search/pagination controls. Use this directly as an
/// Index view's <c>@model</c> so list views stay uniform across controllers.
/// </summary>
public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required PageInfo Page { get; init; }
}
