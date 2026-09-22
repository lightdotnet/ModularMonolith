using Light.Exceptions;

namespace StarterKit.Inventory.Contracts.Exceptions;

/// <summary>
/// A stock decrement was refused because it would take a product below zero on hand. Derives from
/// <see cref="ConflictException"/> so it still maps to a 409 and existing <c>catch (ConflictException)</c>
/// blocks keep working, but it lets a caller tell a deterministic shortage apart from the transient
/// "Stock was modified concurrently" conflict without inspecting the message.
/// </summary>
public class InsufficientStockException(string message) : ConflictException(message);
