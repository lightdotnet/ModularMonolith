using Inventory.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Locations.Contracts.Services;
using Xunit;

namespace Inventory.Tests.Application.StockAdjustments.Commands;

public class RevalueStockCommandHandlerTests
{
    private const string LocationId = "location-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Mock<ILocationDirectoryService> MakeLocationServiceMock(bool locationExists = true)
    {
        var mock = new Mock<ILocationDirectoryService>();
        mock.Setup(s => s.ExistsAsync(LocationId, It.IsAny<CancellationToken>())).ReturnsAsync(locationExists);
        return mock;
    }

    private static async Task SeedAsync(
        StockLedger ledger,
        int quantity,
        decimal unitCost)
    {
        await ledger.ApplyAsync(
            [
                new StockMovement(
                    1,
                    LocationId,
                    quantity,
                    StockAdjustmentReason.ManualAdjustment,
                    "seed-user",
                    UnitCostBase: unitCost),
            ],
            Ct);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var handler = new RevalueStockCommandHandler(ledger, MakeLocationServiceMock(locationExists: false).Object);

        // Act
        var result = await handler.Handle(
            new RevalueStockCommand(new RevalueStockRequest { ProductId = 1, LocationId = LocationId, UnitCost = 3m }, "user-1"),
            Ct);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task Handle_ShouldRevalueTheOnHandStock_AndRecordAQuantityNeutralEntry()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedAsync(ledger, 10, 2m);
        var handler = new RevalueStockCommandHandler(ledger, MakeLocationServiceMock().Object);

        // Act
        var result = await handler.Handle(
            new RevalueStockCommand(
                new RevalueStockRequest { ProductId = 1, LocationId = LocationId, UnitCost = 3m, Note = "supplier repricing" },
                "user-2"),
            Ct);

        // Assert
        Assert.True(result.IsSuccess);

        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(10, level.QuantityOnHand);
        Assert.Equal(30m, level.TotalValueBase);

        var entry = await host.Context.StockAdjustments
            .AsNoTracking()
            .SingleAsync(x => x.Reason == StockAdjustmentReason.CostRevaluation, Ct);
        Assert.Equal(0, entry.QuantityDelta);
        Assert.Equal(3m, entry.UnitCostBase);
        Assert.Equal(10m, entry.ValueDeltaBase);
        Assert.Equal("user-2", entry.PerformedByUserId);
        Assert.Equal("supplier repricing", entry.Note);
        Assert.Null(entry.SourceType);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenNothingIsOnHand()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var handler = new RevalueStockCommandHandler(ledger, MakeLocationServiceMock().Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new RevalueStockCommand(new RevalueStockRequest { ProductId = 1, LocationId = LocationId, UnitCost = 3m }, "user-1"),
            Ct));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }
}
