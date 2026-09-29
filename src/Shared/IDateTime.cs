namespace StarterKit.Shared;

public interface IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset AuditTime => DateTimeOffset.UtcNow;
}
