using Light.Domain.ValueObjects;

namespace Monolith.Identity.Domain;

public class Status : ValueObject
{
    public Status() { }

    public Status(IdentityStatus status)
    {
        Value = status;
    }

    public IdentityStatus Value { get; set; } = IdentityStatus.active;

    public bool IsUnactive => Value == IdentityStatus.unactive;

    public bool IsActive => Value == IdentityStatus.active;

    public bool IsLocked => Value == IdentityStatus.locked;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Using a yield return statement to return each element one at a time
        yield return Value;
        yield return IsUnactive;
        yield return IsActive;
        yield return IsLocked;
    }

    public void Update(IdentityStatus status)
    {
        Value = status;
    }
}