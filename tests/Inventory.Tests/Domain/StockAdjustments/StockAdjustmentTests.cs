using Light.Exceptions;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using Xunit;

namespace Inventory.Tests.Domain.StockAdjustments;

public class StockAdjustmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldThrowValidationException_WhenProductIdIsNotPositive()
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            0, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, "user-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldThrowValidationException_WhenLocationIdIsEmpty(string? locationId)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, locationId!, 5, StockAdjustmentReason.ManualAdjustment, Now, "user-1"));
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenQuantityDeltaIsZero()
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 0, StockAdjustmentReason.ManualAdjustment, Now, "user-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldThrowValidationException_WhenPerformedByUserIdIsEmpty(string? performedByUserId)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, performedByUserId!));
    }

    [Theory]
    [InlineData(StockAdjustmentReason.OrderPlacement)]
    [InlineData(StockAdjustmentReason.OrderCancellationRestore)]
    public void Create_ShouldThrowValidationException_WhenOrderDrivenReason_HasNoSourceOrderId(
        StockAdjustmentReason reason)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", -5, reason, Now, "user-1",
            sourceType: StockSourceType.Order,
            sourceId: null,
            reversesAdjustmentId: reason == StockAdjustmentReason.OrderCancellationRestore ? 1 : null));
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenReversalHasNoReversesAdjustmentId()
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.OrderCancellationRestore, Now, "user-1",
            sourceType: StockSourceType.Order,
            sourceId: 10,
            reversesAdjustmentId: null));
    }

    [Fact]
    public void Create_ShouldSucceed_ForManualAdjustment_WithoutASourceOrder()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, "user-1");

        Assert.Equal(1, adjustment.ProductId);
        Assert.Equal("location-1", adjustment.LocationId);
        Assert.Equal(5, adjustment.QuantityDelta);
        Assert.Equal(StockAdjustmentReason.ManualAdjustment, adjustment.Reason);
        Assert.Null(adjustment.SourceId);
        Assert.Null(adjustment.ReversesAdjustmentId);
    }

    [Fact]
    public void Create_ShouldSucceed_ForOrderPlacement_WithASourceOrder()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", -3, StockAdjustmentReason.OrderPlacement, Now, "user-1",
            sourceType: StockSourceType.Order,
            sourceId: 42,
            sourceLineId: 7,
            idempotencyKey: "order-place:42:7");

        Assert.Equal(42, adjustment.SourceId);
        Assert.Equal(7, adjustment.SourceLineId);
        Assert.Equal("order-place:42:7", adjustment.IdempotencyKey);
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenSourceIdIsGiven_WithoutASourceType()
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, "user-1",
            sourceType: null,
            sourceId: 42));
    }

    [Theory]
    [InlineData(StockAdjustmentReason.OrderPlacement)]
    [InlineData(StockAdjustmentReason.OrderCancellationRestore)]
    public void Create_ShouldThrowValidationException_WhenOrderDrivenReason_HasNoSourceType(
        StockAdjustmentReason reason)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", -5, reason, Now, "user-1",
            sourceType: null,
            sourceId: 42,
            reversesAdjustmentId: reason == StockAdjustmentReason.OrderCancellationRestore ? 1 : null));
    }

    [Theory]
    [InlineData(StockAdjustmentReason.OrderPlacement)]
    [InlineData(StockAdjustmentReason.OrderCancellationRestore)]
    public void Create_ShouldThrowValidationException_WhenOrderDrivenReason_HasANonOrderSourceType(
        StockAdjustmentReason reason)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", -5, reason, Now, "user-1",
            sourceType: StockSourceType.Transfer,
            sourceId: 42,
            reversesAdjustmentId: reason == StockAdjustmentReason.OrderCancellationRestore ? 1 : null));
    }

    [Fact]
    public void Create_ShouldSucceed_ForManualAdjustment_WithNeitherSourceTypeNorSourceId()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, "user-1");

        Assert.Null(adjustment.SourceType);
        Assert.Null(adjustment.SourceId);
    }

    [Fact]
    public void Create_ShouldKeepTheSourceType_ForOrderPlacement()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", -3, StockAdjustmentReason.OrderPlacement, Now, "user-1",
            sourceType: StockSourceType.Order,
            sourceId: 42);

        Assert.Equal(StockSourceType.Order, adjustment.SourceType);
    }

    [Fact]
    public void ReleaseIdempotencyKey_ShouldClearTheKey()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", -3, StockAdjustmentReason.OrderPlacement, Now, "user-1",
            sourceType: StockSourceType.Order,
            sourceId: 42,
            sourceLineId: 7,
            idempotencyKey: "order-place:42:7");

        adjustment.ReleaseIdempotencyKey();

        Assert.Null(adjustment.IdempotencyKey);
    }
}
