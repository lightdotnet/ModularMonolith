using Inventory.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Locations.Contracts.Services;
using Xunit;

namespace Inventory.Tests.Services;

/// <summary>
/// Covers the generic stock seam of <see cref="InventoryService"/> (<c>ReceiveStockAsync</c>,
/// <c>IssueStockAsync</c>, the posting results and the unreversed-postings filter) against a real
/// Sqlite ledger; only the cross-module <see cref="ILocationDirectoryService"/> is mocked.
/// </summary>
public class InventoryServiceStockSeamTests
{
    private const string LocationId = "location-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TheoryData<StockSourceType, StockAdjustmentReason, string> ReceiveSources => new()
    {
        { StockSourceType.GoodsReceipt, StockAdjustmentReason.PurchaseReceipt, "goods-receipt" },
        { StockSourceType.TransferReceipt, StockAdjustmentReason.TransferIn, "transfer-receipt" },
    };

    public static TheoryData<StockSourceType, StockAdjustmentReason, string> IssueSources => new()
    {
        { StockSourceType.Transfer, StockAdjustmentReason.TransferOut, "transfer-issue" },
        { StockSourceType.PurchaseReturn, StockAdjustmentReason.PurchaseReturnOut, "purchase-return" },
    };

    public static TheoryData<StockSourceType> InvalidReceiveSources => new()
    {
        StockSourceType.Order,
        StockSourceType.Transfer,
        StockSourceType.PurchaseReturn,
        (StockSourceType)99,
    };

    public static TheoryData<StockSourceType> InvalidIssueSources => new()
    {
        StockSourceType.Order,
        StockSourceType.GoodsReceipt,
        StockSourceType.TransferReceipt,
        (StockSourceType)99,
    };

    private static (InventoryService Service, StockLedger Ledger) CreateSut(
        InventoryTestHost host,
        bool locationExists = true)
    {
        var locations = new Mock<ILocationDirectoryService>();
        locations.Setup(s => s.ExistsAsync(LocationId, It.IsAny<CancellationToken>())).ReturnsAsync(locationExists);

        var ledger = new StockLedger(host.Context, host.DateTime);

        return (new InventoryService(ledger, locations.Object), ledger);
    }

    private static StockInLine In(
        long productId = 1,
        long lineId = 1,
        int quantity = 1,
        decimal unitCost = 1m,
        string? reference = null) =>
        new(productId, lineId, quantity, unitCost, reference ?? $"ref-{lineId}");

    private static StockOutLine Out(
        long productId = 1,
        long lineId = 1,
        int quantity = 1,
        string? reference = null) =>
        new(productId, lineId, quantity, reference ?? $"ref-{lineId}");

    private static async Task SeedAsync(
        StockLedger ledger,
        long productId,
        int quantity,
        decimal unitCost)
    {
        await ledger.ApplyAsync(
            [
                new StockMovement(
                    productId,
                    LocationId,
                    quantity,
                    StockAdjustmentReason.ManualAdjustment,
                    "seed-user",
                    UnitCostBase: unitCost),
            ],
            Ct);
    }

    [Theory]
    [MemberData(nameof(ReceiveSources))]
    public async Task ReceiveStockAsync_ShouldPostEachLineAtItsCost_WithTheSourceKeyAndReason(
        StockSourceType sourceType,
        StockAdjustmentReason reason,
        string keyPrefix)
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        // Act
        var results = await service.ReceiveStockAsync(
            sourceType,
            7,
            LocationId,
            [In(productId: 1, lineId: 10, quantity: 4, unitCost: 2.5m, reference: "a"), In(productId: 2, lineId: 11, quantity: 2, unitCost: 3m, reference: "b")],
            "user-1",
            Ct);

        // Assert
        Assert.Equal(
            [new StockPostingResult(1, 10, 4, 2.5m, 10m), new StockPostingResult(2, 11, 2, 3m, 6m)],
            results);

        var adjustments = await host.Context.StockAdjustments.AsNoTracking().OrderBy(x => x.ProductId).ToListAsync(Ct);
        Assert.All(adjustments, x =>
        {
            Assert.Equal(reason, x.Reason);
            Assert.Equal(sourceType, x.SourceType);
            Assert.Equal(7, x.SourceId);
            Assert.Equal("user-1", x.PerformedByUserId);
        });
        Assert.Equal($"{keyPrefix}:7:a", adjustments[0].IdempotencyKey);
        Assert.Equal($"{keyPrefix}:7:b", adjustments[1].IdempotencyKey);
        Assert.Equal(10, adjustments[0].SourceLineId!.Value);

        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(x => x.ProductId == 1, Ct);
        Assert.Equal(4, level.QuantityOnHand);
        Assert.Equal(10m, level.TotalValueBase);
    }

    [Theory]
    [MemberData(nameof(IssueSources))]
    public async Task IssueStockAsync_ShouldPostEachLineAtTheAverage_WithTheSourceKeyAndReason(
        StockSourceType sourceType,
        StockAdjustmentReason reason,
        string keyPrefix)
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 2m);

        // Act
        var results = await service.IssueStockAsync(
            sourceType,
            8,
            LocationId,
            [Out(productId: 1, lineId: 20, quantity: 4, reference: "x")],
            "user-1",
            Ct);

        // Assert — reported as positive amounts although they left stock.
        Assert.Equal([new StockPostingResult(1, 20, 4, 2m, 8m)], results);

        var adjustment = await host.Context.StockAdjustments
            .AsNoTracking()
            .SingleAsync(x => x.Reason == reason, Ct);
        Assert.Equal(sourceType, adjustment.SourceType);
        Assert.Equal(8, adjustment.SourceId);
        Assert.Equal(-4, adjustment.QuantityDelta);
        Assert.Equal(-8m, adjustment.ValueDeltaBase);
        Assert.Equal($"{keyPrefix}:8:x", adjustment.IdempotencyKey);

        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(6, level.QuantityOnHand);
        Assert.Equal(12m, level.TotalValueBase);
    }

    [Theory]
    [MemberData(nameof(InvalidReceiveSources))]
    public async Task ReceiveStockAsync_ShouldThrowArgumentOutOfRange_WhenTheSourceTypeCannotReceive(StockSourceType sourceType)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ReceiveStockAsync(sourceType, 1, LocationId, [In()], "user-1", Ct));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Theory]
    [MemberData(nameof(InvalidIssueSources))]
    public async Task IssueStockAsync_ShouldThrowArgumentOutOfRange_WhenTheSourceTypeCannotIssue(StockSourceType sourceType)
    {
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 5, 1m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.IssueStockAsync(sourceType, 1, LocationId, [Out()], "user-1", Ct));

        Assert.Equal(5, (await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct)).QuantityOnHand);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldReturnTheOriginalResults_WhenReplayedAfterTheAverageChanged()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);
        var lines = new List<StockInLine> { In(productId: 1, lineId: 1, quantity: 10, unitCost: 2m, reference: "a") };
        var original = await service.ReceiveStockAsync(StockSourceType.GoodsReceipt, 1, LocationId, lines, "user-1", Ct);
        await service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt,
            2,
            LocationId,
            [In(productId: 1, lineId: 1, quantity: 10, unitCost: 10m, reference: "a")],
            "user-1",
            Ct);

        // Act — same document again, but with a different unit cost on the replayed line.
        var replay = await service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt,
            1,
            LocationId,
            [In(productId: 1, lineId: 1, quantity: 10, unitCost: 99m, reference: "a")],
            "user-1",
            Ct);

        // Assert
        Assert.Equal([new StockPostingResult(1, 1, 10, 2m, 20m)], original);
        Assert.Equal(original, replay);

        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(20, level.QuantityOnHand);
        Assert.Equal(120m, level.TotalValueBase);
        Assert.Equal(2, await host.Context.StockAdjustments.CountAsync(Ct));
    }

    [Fact]
    public async Task IssueStockAsync_ShouldReturnTheOriginalResults_WhenReplayedAfterTheAverageChanged()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 2m);
        var lines = new List<StockOutLine> { Out(productId: 1, lineId: 1, quantity: 4, reference: "a") };
        var original = await service.IssueStockAsync(StockSourceType.Transfer, 5, LocationId, lines, "user-1", Ct);
        await SeedAsync(ledger, 1, 10, 10m);

        // Act
        var replay = await service.IssueStockAsync(StockSourceType.Transfer, 5, LocationId, lines, "user-1", Ct);

        // Assert
        Assert.Equal([new StockPostingResult(1, 1, 4, 2m, 8m)], original);
        Assert.Equal(original, replay);

        var level = await host.Context.StockLevels.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(16, level.QuantityOnHand);
        Assert.Equal(112m, level.TotalValueBase);
    }

    [Fact]
    public async Task DecrementForOrderAsync_ShouldReturnThePostedCost_AndTheSameOnReplay()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 2m);
        var lines = new List<StockLine> { new(1, 30, 3) };
        var original = await service.DecrementForOrderAsync(100, LocationId, lines, "user-1", Ct);
        await SeedAsync(ledger, 1, 10, 10m);

        // Act
        var replay = await service.DecrementForOrderAsync(100, LocationId, lines, "user-1", Ct);

        // Assert
        Assert.Equal([new StockPostingResult(1, 30, 3, 2m, 6m)], original);
        Assert.Equal(original, replay);
    }

    [Fact]
    public async Task IssueStockAsync_ShouldApplyNothing_AndReportEveryShortfallInOneConflict_AcrossProducts()
    {
        // Arrange — two products are short, one has plenty.
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 1, 1m);
        await SeedAsync(ledger, 2, 1, 1m);
        await SeedAsync(ledger, 3, 10, 1m);

        // Act
        var exception = await Assert.ThrowsAsync<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => service.IssueStockAsync(
            StockSourceType.Transfer,
            9,
            LocationId,
            [Out(productId: 1, lineId: 1, quantity: 5), Out(productId: 2, lineId: 2, quantity: 5), Out(productId: 3, lineId: 3, quantity: 2)],
            "user-1",
            Ct));

        // Assert
        Assert.Contains("1 at location-1", exception.Message);
        Assert.Contains("2 at location-1", exception.Message);
        Assert.DoesNotContain("3 at location-1", exception.Message);

        var levels = await host.Context.StockLevels.AsNoTracking().OrderBy(x => x.ProductId).ToListAsync(Ct);
        Assert.Equal(new[] { 1, 1, 10 }, levels.Select(x => x.QuantityOnHand));
        Assert.False(await host.Context.StockAdjustments.AnyAsync(x => x.SourceType == StockSourceType.Transfer, Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldReturnEmpty_WhenThereAreNoLines()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        var results = await service.ReceiveStockAsync(StockSourceType.GoodsReceipt, 1, LocationId, [], "user-1", Ct);

        Assert.Empty(results);
    }

    [Fact]
    public async Task IssueStockAsync_ShouldReturnEmpty_WhenThereAreNoLines()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        var results = await service.IssueStockAsync(StockSourceType.Transfer, 1, LocationId, [], "user-1", Ct);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenAnIdempotencyRefIsBlank(string reference)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In(reference: reference)], "user-1", Ct));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenAnIdempotencyRefIsBlank(string reference)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, 1, LocationId, [Out(reference: reference)], "user-1", Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenIdempotencyRefsAreDuplicated()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt,
            1,
            LocationId,
            [In(productId: 1, lineId: 1, reference: "dup"), In(productId: 2, lineId: 2, reference: "dup")],
            "user-1",
            Ct));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenIdempotencyRefsAreDuplicated()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer,
            1,
            LocationId,
            [Out(productId: 1, lineId: 1, reference: "dup"), Out(productId: 2, lineId: 2, reference: "dup")],
            "user-1",
            Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldTreatRefsThatDifferOnlyByCaseAsDistinct()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        var results = await service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt,
            1,
            LocationId,
            [In(productId: 1, lineId: 1, reference: "A"), In(productId: 2, lineId: 2, reference: "a")],
            "user-1",
            Ct);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldAcceptTheLongestRefThatFitsTheKeyColumn_AndRejectOneMore()
    {
        // Arrange — the key "goods-receipt:{sourceId}:{ref}" must stay within 200 characters.
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);
        var maxRefLength = 200 - "goods-receipt".Length - "7".Length - 2;

        // Act
        var results = await service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 7, LocationId, [In(reference: new string('r', maxRefLength))], "user-1", Ct);

        // Assert
        Assert.Single(results);
        Assert.Equal(200, (await host.Context.StockAdjustments.AsNoTracking().SingleAsync(Ct)).IdempotencyKey!.Length);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 8, LocationId, [In(reference: new string('r', maxRefLength + 1))], "user-1", Ct));
    }

    [Fact]
    public async Task IssueStockAsync_ShouldRejectARefThatMakesTheKeyTooLong()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);
        var maxRefLength = 200 - "transfer-issue".Length - "7".Length - 2;

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, 7, LocationId, [Out(reference: new string('r', maxRefLength + 1))], "user-1", Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenTheSourceIdIsNotPositive(long sourceId)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, sourceId, LocationId, [In()], "user-1", Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenTheSourceIdIsNotPositive(long sourceId)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, sourceId, LocationId, [Out()], "user-1", Ct));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenPerformedByIsBlank(string? performedBy)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In()], performedBy!, Ct));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenPerformedByIsBlank(string? performedBy)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, 1, LocationId, [Out()], performedBy!, Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenTheLocationIsUnknown()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host, locationExists: false);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In()], "user-1", Ct));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenTheLocationIsUnknown()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host, locationExists: false);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, 1, LocationId, [Out()], "user-1", Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenAQuantityIsNotPositive(int quantity)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In(quantity: quantity)], "user-1", Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task IssueStockAsync_ShouldThrowValidationException_WhenAQuantityIsNotPositive(int quantity)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.IssueStockAsync(
            StockSourceType.Transfer, 1, LocationId, [Out(quantity: quantity)], "user-1", Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldThrowValidationException_WhenAUnitCostIsNegative()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ValidationException>(() => service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In(unitCost: -0.0001m)], "user-1", Ct));
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldAcceptAZeroUnitCost()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        var results = await service.ReceiveStockAsync(
            StockSourceType.GoodsReceipt, 1, LocationId, [In(quantity: 3, unitCost: 0m)], "user-1", Ct);

        Assert.Equal([new StockPostingResult(1, 1, 3, 0m, 0m)], results);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldFindGoodsReceipts_AndIgnoreOtherSourceTypes()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);
        await service.ReceiveStockAsync(StockSourceType.GoodsReceipt, 11, LocationId, [In()], "user-1", Ct);
        await service.ReceiveStockAsync(StockSourceType.TransferReceipt, 12, LocationId, [In()], "user-1", Ct);

        // Act
        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.GoodsReceipt, [11, 12, 13], host.DateTime.UtcNow.AddMinutes(5), Ct);

        // Assert
        Assert.Equal(new long[] { 11 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldFindTransferReceipts()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);
        await service.ReceiveStockAsync(StockSourceType.TransferReceipt, 12, LocationId, [In()], "user-1", Ct);
        await service.ReceiveStockAsync(StockSourceType.GoodsReceipt, 13, LocationId, [In(lineId: 2)], "user-1", Ct);

        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.TransferReceipt, [12, 13], host.DateTime.UtcNow.AddMinutes(5), Ct);

        Assert.Equal(new long[] { 12 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldFindTransfers()
    {
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 1m);
        await service.IssueStockAsync(StockSourceType.Transfer, 21, LocationId, [Out()], "user-1", Ct);
        await service.IssueStockAsync(StockSourceType.PurchaseReturn, 22, LocationId, [Out(lineId: 2)], "user-1", Ct);

        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Transfer, [21, 22], host.DateTime.UtcNow.AddMinutes(5), Ct);

        Assert.Equal(new long[] { 21 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldFindPurchaseReturns()
    {
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 1m);
        await service.IssueStockAsync(StockSourceType.PurchaseReturn, 22, LocationId, [Out()], "user-1", Ct);
        await service.IssueStockAsync(StockSourceType.Transfer, 23, LocationId, [Out(lineId: 2)], "user-1", Ct);

        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.PurchaseReturn, [22, 23], host.DateTime.UtcNow.AddMinutes(5), Ct);

        Assert.Equal(new long[] { 22 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldFindOrders_ButNotOnceReversed()
    {
        using var host = new InventoryTestHost();
        var (service, ledger) = CreateSut(host);
        await SeedAsync(ledger, 1, 10, 1m);
        await service.DecrementForOrderAsync(31, LocationId, [new StockLine(1, 1, 1)], "user-1", Ct);
        await service.DecrementForOrderAsync(32, LocationId, [new StockLine(1, 1, 1)], "user-1", Ct);
        await service.RestoreForOrderAsync(32, "user-1", Ct);

        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order, [31, 32], host.DateTime.UtcNow.AddMinutes(5), Ct);

        Assert.Equal(new long[] { 31 }, result);
    }

    [Theory]
    [InlineData(StockSourceType.GoodsReceipt)]
    [InlineData(StockSourceType.TransferReceipt)]
    [InlineData(StockSourceType.Transfer)]
    [InlineData(StockSourceType.PurchaseReturn)]
    [InlineData(StockSourceType.Order)]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldReturnEmpty_WhenNothingWasPosted(StockSourceType sourceType)
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        var result = await service.FilterSourceIdsWithUnreversedPostingsAsync(
            sourceType, [1, 2], host.DateTime.UtcNow.AddMinutes(5), Ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldThrowArgumentOutOfRange_ForAnUnknownSourceType()
    {
        using var host = new InventoryTestHost();
        var (service, _) = CreateSut(host);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.FilterSourceIdsWithUnreversedPostingsAsync(
            (StockSourceType)99, [1], host.DateTime.UtcNow, Ct));
    }
}
