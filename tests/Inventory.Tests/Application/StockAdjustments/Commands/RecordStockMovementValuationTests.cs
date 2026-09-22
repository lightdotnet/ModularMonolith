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

/// <summary>Covers how a manual stock adjustment is priced: explicit cost, average default, outbound at average.</summary>
public class RecordStockMovementValuationTests
{
    private const string LocationId = "location-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static RecordStockMovementCommandHandler CreateHandler(InventoryTestHost host)
    {
        var locations = new Mock<ILocationDirectoryService>();
        locations.Setup(s => s.ExistsAsync(LocationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        return new RecordStockMovementCommandHandler(new StockLedger(host.Context, host.DateTime), locations.Object);
    }

    private static RecordStockMovementCommand Command(
        int quantity,
        decimal? unitCost = null,
        string performedBy = "user-1") =>
        new(
            new RecordStockMovementRequest
            {
                ProductId = 1,
                LocationId = LocationId,
                QuantityDelta = quantity,
                UnitCost = unitCost,
            },
            performedBy);

    [Fact]
    public async Task Handle_ShouldPriceAnInboundAtTheGivenCost_AsAManualAdjustmentWithNoSource()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var handler = CreateHandler(host);

        // Act
        var result = await handler.Handle(Command(4, unitCost: 2.5m), Ct);

        // Assert
        Assert.True(result.IsSuccess);
        var entry = await host.Context.StockAdjustments.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(StockAdjustmentReason.ManualAdjustment, entry.Reason);
        Assert.Null(entry.SourceType);
        Assert.Null(entry.SourceId);
        Assert.Null(entry.IdempotencyKey);
        Assert.Equal(2.5m, entry.UnitCostBase);
        Assert.Equal(10m, entry.ValueDeltaBase);
    }

    [Fact]
    public async Task Handle_ShouldDefaultAnInboundWithoutACostToTheCurrentAverage()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var handler = CreateHandler(host);
        await handler.Handle(Command(10, unitCost: 3m), Ct);

        // Act
        await handler.Handle(Command(4), Ct);

        // Assert
        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(14, level.QuantityOnHand);
        Assert.Equal(42m, level.TotalValueBase);
        Assert.Equal(3m, level.AverageCostBase);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenAnInboundHasNoCost_AndNothingIsOnHand()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var handler = CreateHandler(host);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(4), Ct));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task Handle_ShouldPriceAnOutboundAtTheAverage_IgnoringAGivenCost()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var handler = CreateHandler(host);
        await handler.Handle(Command(10, unitCost: 3m), Ct);

        // Act
        await handler.Handle(Command(-2, unitCost: 99m), Ct);

        // Assert
        var entry = await host.Context.StockAdjustments
            .AsNoTracking()
            .SingleAsync(x => x.QuantityDelta == -2, Ct);
        Assert.Equal(3m, entry.UnitCostBase);
        Assert.Equal(-6m, entry.ValueDeltaBase);
    }

    [Fact]
    public async Task Handle_ShouldRecordThePerformingUser()
    {
        using var host = new InventoryTestHost();
        var handler = CreateHandler(host);

        await handler.Handle(Command(1, unitCost: 1m, performedBy: "user-9"), Ct);

        Assert.Equal("user-9", (await host.Context.StockAdjustments.AsNoTracking().SingleAsync(Ct)).PerformedByUserId);
    }
}
