using Light.Exceptions;
using StarterKit.Inventory.Contracts.Exceptions;
using Xunit;

namespace Inventory.Tests.Domain;

public class InsufficientStockExceptionTests
{
    [Fact]
    public void InsufficientStockException_ShouldDeriveFromConflictException()
    {
        Assert.IsAssignableFrom<ConflictException>(new InsufficientStockException("short"));
    }

    [Fact]
    public void TransientConcurrencyConflict_ShouldNotBeAnInsufficientStockException()
    {
        Exception transient = new ConflictException("Stock was modified concurrently");

        Assert.IsNotType<InsufficientStockException>(transient);
        Assert.False(transient is InsufficientStockException);
    }
}
