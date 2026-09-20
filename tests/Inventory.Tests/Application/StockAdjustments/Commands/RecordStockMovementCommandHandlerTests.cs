using Inventory.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Locations.Contracts.Services;
using Xunit;

namespace Inventory.Tests.Application.StockAdjustments.Commands;

public class RecordStockMovementCommandHandlerTests
{
    private const string LocationId = "location-1";

    private static Mock<ILocationDirectoryService> MakeLocationServiceMock(bool locationExists = true)
    {
        var mock = new Mock<ILocationDirectoryService>();
        mock.Setup(s => s.ExistsAsync(LocationId, It.IsAny<CancellationToken>())).ReturnsAsync(locationExists);
        return mock;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var handler = new RecordStockMovementCommandHandler(ledger, MakeLocationServiceMock(locationExists: false).Object);

        // Act
        var result = await handler.Handle(
            new RecordStockMovementCommand(
                new RecordStockMovementRequest { ProductId = 1, LocationId = LocationId, QuantityDelta = 5 },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(await host.Context.StockAdjustments.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldRecordAManualAdjustment_AndReturnSuccess_WhenLocationExists()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var handler = new RecordStockMovementCommandHandler(ledger, MakeLocationServiceMock().Object);

        // Act
        var result = await handler.Handle(
            new RecordStockMovementCommand(
                new RecordStockMovementRequest { ProductId = 1, LocationId = LocationId, QuantityDelta = 8, Note = "initial count" },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(8, level.QuantityOnHand);
        var adjustment = await host.Context.StockAdjustments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(StockAdjustmentReason.ManualAdjustment, adjustment.Reason);
        Assert.Equal("user-1", adjustment.PerformedByUserId);
        Assert.Equal("initial count", adjustment.Note);
        Assert.Null(adjustment.SourceOrderId);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenTheManualAdjustmentWouldGoNegative()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var handler = new RecordStockMovementCommandHandler(ledger, MakeLocationServiceMock().Object);

        // Act & Assert — no existing stock row, so any negative delta is an immediate shortfall.
        await Assert.ThrowsAsync<Light.Exceptions.ConflictException>(() => handler.Handle(
            new RecordStockMovementCommand(
                new RecordStockMovementRequest { ProductId = 1, LocationId = LocationId, QuantityDelta = -1 },
                "user-1"),
            TestContext.Current.CancellationToken));
    }
}
