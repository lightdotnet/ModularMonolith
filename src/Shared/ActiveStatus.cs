using Light.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace StarterKit.Shared;

public class ActiveStatus : ValueObject
{
    public ActiveStatus() { }

    public ActiveStatus(State status)
    {
        Value = status;
    }

    public enum State
    {
        Inactive = 0,
        Active = 1,
        Locked = 2,
    }

    public State Value { get; set; } = State.Active;

    [JsonIgnore]
    public bool IsInactive => Value == State.Inactive;

    [JsonIgnore]
    public bool IsActive => Value == State.Active;

    [JsonIgnore]
    public bool IsLocked => Value == State.Locked;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public void Update(State status)
    {
        Value = status;
    }
}
