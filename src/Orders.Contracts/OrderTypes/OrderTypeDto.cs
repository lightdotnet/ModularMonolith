using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.OrderTypes;

/// <summary>
/// Not a <c>BaseDto&lt;TId&gt;</c> — that base assumes a single scalar <c>Id</c> is the whole
/// identity. This catalog's identity is the composite <c>(Id, Category)</c> pair, so both are plain
/// members here instead.
/// </summary>
public class OrderTypeDto
{
    public string Id { get; set; } = null!;

    public OrderTypeCategory Category { get; set; }

    public string Name { get; set; } = null!;

    public OrderTypeStatus Status { get; set; }
}
