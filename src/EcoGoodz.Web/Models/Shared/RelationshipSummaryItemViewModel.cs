namespace EcoGoodz.Web.Models.Shared;

public class RelationshipSummaryItemViewModel
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public string? Controller { get; init; }
    public string? Action { get; init; }
    public Dictionary<string, string>? RouteValues { get; init; }
}
