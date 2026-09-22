using Light.Exceptions;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Common;
using Xunit;

namespace Inventory.Tests.Domain.StockAdjustments;

/// <summary>Covers the cost-revaluation and source-type rules of <see cref="StockAdjustment.Create"/>.</summary>
public class StockAdjustmentValuationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<StockAdjustmentReason, StockSourceType> SourceDrivenReasons => new()
    {
        { StockAdjustmentReason.OrderPlacement, StockSourceType.Order },
        { StockAdjustmentReason.OrderCancellationRestore, StockSourceType.Order },
        { StockAdjustmentReason.PurchaseReceipt, StockSourceType.GoodsReceipt },
        { StockAdjustmentReason.TransferOut, StockSourceType.Transfer },
        { StockAdjustmentReason.TransferIn, StockSourceType.TransferReceipt },
        { StockAdjustmentReason.PurchaseReturnOut, StockSourceType.PurchaseReturn },
    };

    private static StockAdjustment CreateSourceDriven(
        StockAdjustmentReason reason,
        StockSourceType? sourceType,
        long? sourceId)
    {
        return StockAdjustment.Create(
            1,
            "location-1",
            reason is StockAdjustmentReason.PurchaseReceipt or StockAdjustmentReason.TransferIn ? 5 : -5,
            reason,
            Now,
            "user-1",
            sourceType: sourceType,
            sourceId: sourceId,
            reversesAdjustmentId: reason == StockAdjustmentReason.OrderCancellationRestore ? 1 : null);
    }

    [Fact]
    public void Create_ShouldSucceed_ForCostRevaluation_WithZeroQuantityAndAPositiveUnitCost()
    {
        var adjustment = StockAdjustment.Create(
            1,
            "location-1",
            0,
            StockAdjustmentReason.CostRevaluation,
            Now,
            "user-1",
            unitCostBase: 3.5m,
            valueDeltaBase: 10m);

        Assert.Equal(0, adjustment.QuantityDelta);
        Assert.Equal(3.5m, adjustment.UnitCostBase);
        Assert.Equal(10m, adjustment.ValueDeltaBase);
        Assert.Null(adjustment.SourceType);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void Create_ShouldThrowValidationException_WhenCostRevaluationMovesQuantity(int quantityDelta)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", quantityDelta, StockAdjustmentReason.CostRevaluation, Now, "user-1",
            unitCostBase: 3m));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Create_ShouldThrowValidationException_WhenCostRevaluationHasNoPositiveUnitCost(string unitCost)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 0, StockAdjustmentReason.CostRevaluation, Now, "user-1",
            unitCostBase: decimal.Parse(unitCost)));
    }

    [Theory]
    [InlineData(StockAdjustmentReason.ManualAdjustment)]
    [InlineData(StockAdjustmentReason.PurchaseReceipt)]
    public void Create_ShouldStillRejectZeroQuantity_ForEveryReasonExceptCostRevaluation(StockAdjustmentReason reason)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 0, reason, Now, "user-1",
            sourceType: reason == StockAdjustmentReason.PurchaseReceipt ? StockSourceType.GoodsReceipt : null,
            sourceId: reason == StockAdjustmentReason.PurchaseReceipt ? 5 : null));
    }

    [Theory]
    [MemberData(nameof(SourceDrivenReasons))]
    public void Create_ShouldSucceed_WhenSourceDrivenReason_HasTheMatchingSource(
        StockAdjustmentReason reason,
        StockSourceType sourceType)
    {
        var adjustment = CreateSourceDriven(reason, sourceType, sourceId: 42);

        Assert.Equal(reason, adjustment.Reason);
        Assert.Equal(sourceType, adjustment.SourceType);
        Assert.Equal(42, adjustment.SourceId);
    }

    [Theory]
    [MemberData(nameof(SourceDrivenReasons))]
    public void Create_ShouldThrowValidationException_WhenSourceDrivenReason_HasAMismatchedSourceType(
        StockAdjustmentReason reason,
        StockSourceType expected)
    {
        var wrong = Enum.GetValues<StockSourceType>().First(x => x != expected);

        Assert.Throws<ValidationException>(() => CreateSourceDriven(reason, wrong, sourceId: 42));
    }

    [Theory]
    [MemberData(nameof(SourceDrivenReasons))]
    public void Create_ShouldThrowValidationException_WhenSourceDrivenReason_HasNoSourceType(
        StockAdjustmentReason reason,
        StockSourceType _)
    {
        Assert.Throws<ValidationException>(() => CreateSourceDriven(reason, sourceType: null, sourceId: 42));
    }

    [Theory]
    [MemberData(nameof(SourceDrivenReasons))]
    public void Create_ShouldThrowValidationException_WhenSourceDrivenReason_HasNoSourceId(
        StockAdjustmentReason reason,
        StockSourceType sourceType)
    {
        Assert.Throws<ValidationException>(() => CreateSourceDriven(reason, sourceType, sourceId: null));
    }

    [Theory]
    [InlineData(StockAdjustmentReason.ManualAdjustment)]
    [InlineData(StockAdjustmentReason.CostRevaluation)]
    public void RequiredSourceType_ShouldBeNull_ForReasonsWithoutASource(StockAdjustmentReason reason)
    {
        Assert.Null(StockAdjustment.RequiredSourceType(reason));
    }

    [Theory]
    [MemberData(nameof(SourceDrivenReasons))]
    public void RequiredSourceType_ShouldMapEverySourceDrivenReason(
        StockAdjustmentReason reason,
        StockSourceType expected)
    {
        Assert.Equal(expected, StockAdjustment.RequiredSourceType(reason));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrowValidationException_WhenPerformedByIsBlank_ForCostRevaluation(string? performedBy)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 0, StockAdjustmentReason.CostRevaluation, Now, performedBy!,
            unitCostBase: 2m));
    }

    [Theory]
    [InlineData("   ")]
    public void Create_ShouldThrowValidationException_WhenPerformedByIsWhitespace(string performedBy)
    {
        Assert.Throws<ValidationException>(() => StockAdjustment.Create(
            1, "location-1", 5, StockAdjustmentReason.ManualAdjustment, Now, performedBy));
    }

    [Fact]
    public void Create_ShouldKeepTheCostAndValue_ForAnInboundEntry()
    {
        var adjustment = StockAdjustment.Create(
            1, "location-1", 4, StockAdjustmentReason.PurchaseReceipt, Now, "user-1",
            sourceType: StockSourceType.GoodsReceipt,
            sourceId: 9,
            unitCostBase: 2.5m,
            valueDeltaBase: 10m);

        Assert.Equal(2.5m, adjustment.UnitCostBase);
        Assert.Equal(10m, adjustment.ValueDeltaBase);
    }
}
