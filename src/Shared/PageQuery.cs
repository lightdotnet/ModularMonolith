namespace StarterKit.Shared;

/// <summary>
/// Lookup data entries with pagination
/// </summary>
public record PageQuery : IPage
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
