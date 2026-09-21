namespace EcoGoodz.Web.Models.Shared;

/// <summary>
/// Paging/search metadata for a list page, independent of the item type so a
/// single <c>_Pagination.cshtml</c>/<c>_ListSearchForm.cshtml</c> partial pair
/// can render it for any controller's Index view.
/// </summary>
public sealed class PageInfo
{
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public string? SearchTerm { get; init; }

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
