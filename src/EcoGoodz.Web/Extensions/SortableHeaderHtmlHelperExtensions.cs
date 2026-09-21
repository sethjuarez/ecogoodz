using System.Net;
using EcoGoodz.Web.Models.Shared;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcoGoodz.Web.Extensions;

/// <summary>
/// Renders sortable &lt;th&gt; column headers for any Index view backed by
/// <see cref="PageInfo"/>. Links are built as plain query strings against the
/// current request path (rather than asp-action/asp-route-* tag helpers), so
/// this one helper works unmodified for every list controller - no per-page
/// action/controller name wiring needed. Clicking a header toggles asc/desc on
/// that column, resets to page 1, and preserves the current search term.
/// </summary>
public static class SortableHeaderHtmlHelperExtensions
{
    private const string IconAttributes = "width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    private const string ChevronUp =
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" {IconAttributes} class=\"icon icon-sm ms-1\"><path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M6 15l6 -6l6 6\"/></svg>";

    private const string ChevronDown =
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" {IconAttributes} class=\"icon icon-sm ms-1\"><path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M6 9l6 6l6 -6\"/></svg>";

    private const string Selector =
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" {IconAttributes} class=\"icon icon-sm ms-1 text-secondary\"><path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M8 9l4 -4l4 4\"/><path d=\"M16 15l-4 4l-4 -4\"/></svg>";

    public static IHtmlContent SortableHeader(this IHtmlHelper html, string column, string label, PageInfo page)
    {
        var isActive = string.Equals(page.SortColumn, column, StringComparison.OrdinalIgnoreCase);
        var nextDescending = isActive && !page.SortDescending;

        var query = new List<string> { $"sort={Uri.EscapeDataString(column)}" };
        if (!string.IsNullOrWhiteSpace(page.SearchTerm))
        {
            query.Add($"search={Uri.EscapeDataString(page.SearchTerm)}");
        }
        if (nextDescending)
        {
            query.Add("desc=true");
        }
        if (page.PageSize != PageInfo.DefaultPageSize)
        {
            query.Add($"pageSize={page.PageSize}");
        }

        var href = html.ViewContext.HttpContext.Request.Path + "?" + string.Join("&", query);
        var icon = isActive ? (page.SortDescending ? ChevronDown : ChevronUp) : Selector;

        return new HtmlString(
            $"<a href=\"{WebUtility.HtmlEncode(href)}\" class=\"text-reset d-inline-flex align-items-center\">{WebUtility.HtmlEncode(label)}{icon}</a>");
    }
}
