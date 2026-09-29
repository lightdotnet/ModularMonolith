namespace StarterKit.Shared;

/// <summary>
/// Lookup data entries with pagination and search value
/// </summary>
public record SearchQuery : PageQuery
{
    public string? SearchValue { get; set; }
}
