namespace BhaiGCafe.Api.Models;

public sealed class MenuItem
{
    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public MenuBadge? Badge { get; init; }
}

public sealed class MenuBadge
{
    public string Label { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
}
