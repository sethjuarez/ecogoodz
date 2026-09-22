namespace EcoGoodz.Web.Models.Shared;

public class GlobalSearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<GlobalSearchResultViewModel> Results { get; set; } = [];
}

public class GlobalSearchResultViewModel
{
    public required string Category { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Controller { get; init; }
    public required string Action { get; init; }
    public int? RouteId { get; init; }
}
