using Moq;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;

namespace Purchasing.Tests.TestSupport;

/// <summary>Configures the cross-module <see cref="IInventoryService"/> seam mock used by the posting tests.</summary>
internal static class InventoryMock
{
    /// <summary>The landed-check reports every candidate id as landed (<paramref name="anyLanded"/>) or none.</summary>
    public static Mock<IInventoryService> Create(bool anyLanded = false)
    {
        var mock = new Mock<IInventoryService>();

        mock.Setup(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                StockSourceType _,
                IReadOnlyCollection<long> ids,
                DateTimeOffset _,
                CancellationToken _) =>
                anyLanded
                    ? (IReadOnlyList<long>)ids.OrderBy(x => x).ToList()
                    : (IReadOnlyList<long>)[]);

        return mock;
    }

    /// <summary>Issue succeeds at a flat unit cost for every requested line.</summary>
    public static void SetupIssueAtCost(
        this Mock<IInventoryService> mock,
        decimal unitCost) =>
        mock.Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                StockSourceType _,
                long _,
                string _,
                IReadOnlyList<StockOutLine> lines,
                string _,
                CancellationToken _) =>
                (IReadOnlyList<StockPostingResult>)lines
                    .Select(x => new StockPostingResult(
                        x.ProductId,
                        x.SourceLineId,
                        x.Quantity,
                        unitCost,
                        unitCost * x.Quantity))
                    .ToList());

    public static void SetupIssueThrows(
        this Mock<IInventoryService> mock,
        Exception exception) =>
        mock.Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    public static void SetupReceiveThrows(
        this Mock<IInventoryService> mock,
        Exception exception) =>
        mock.Setup(x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    public static void VerifyReceiveCalls(
        this Mock<IInventoryService> mock,
        Times times) =>
        mock.Verify(
            x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            times);

    public static void VerifyIssueCalls(
        this Mock<IInventoryService> mock,
        Times times) =>
        mock.Verify(
            x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            times);
}
