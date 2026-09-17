using System.Text.Json.Serialization;

namespace StarterKit.Shared;

public abstract class BaseDto<TId>
{
    [JsonPropertyOrder(-1)]
    public TId Id { get; set; } = default!;
}

public abstract class BaseDto : BaseDto<string>
{ }
