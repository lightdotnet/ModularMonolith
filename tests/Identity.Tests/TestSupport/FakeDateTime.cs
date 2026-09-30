using StarterKit.Shared;

namespace Identity.Tests.TestSupport;

public sealed class FakeDateTime : IDateTime
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset AuditTime => UtcNow;
}
