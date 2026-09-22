using StarterKit.Transfers.Api.Application.StockTransfers.Queries;
using StarterKit.Transfers.Contracts.Common;
using StarterKit.Transfers.Contracts.StockTransfers;
using Transfers.Tests.TestSupport;

namespace Transfers.Tests.Application.StockTransfers.Queries;

public class StockTransferQueryHandlerTests
{
    // A dispatched transfer (unit cost 5) with one posted receipt and the remainder closed short.
    private static async Task<long> SeedClosedAsync(TransfersTestHost host)
    {
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10), (2, 4));
        var receipt = transfer.BeginReceive("req-1", [(transfer.Lines[0].Id, 6)], host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        transfer.CompleteReceive(receipt.Id, host.DateTime.UtcNow);
        transfer.Close("lost", host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        TransferSeed.Detach(host);

        return transfer.Id;
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_ForAnUnknownTransfer()
    {
        using var host = new TransfersTestHost();

        var result = await new GetStockTransferByIdQueryHandler(host.Context)
            .Handle(new GetStockTransferByIdQuery(999, true), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Get_ShouldMaskCosts_WithoutViewCost_ButKeepQuantities()
    {
        using var host = new TransfersTestHost();
        var id = await SeedClosedAsync(host);

        var result = await new GetStockTransferByIdQueryHandler(host.Context)
            .Handle(new GetStockTransferByIdQuery(id, false), TestContext.Current.CancellationToken);

        var dto = result.Data!;
        Assert.Null(dto.ClosedShortValueBase);
        Assert.All(dto.Lines, x => Assert.Null(x.UnitCostBase));
        Assert.All(dto.Lines, x => Assert.Null(x.ClosedShortValueBase));
        Assert.Equal(TransferStatus.Closed, dto.Status);
        Assert.Equal(14, dto.TotalRequestedQuantity);
        Assert.Equal(0, dto.TotalInTransitQuantity);
        Assert.Equal(8, dto.Lines.Sum(x => x.QtyClosedShort));
    }

    [Fact]
    public async Task Get_ShouldExposeCosts_WithViewCost_AndIncludeReceipts()
    {
        using var host = new TransfersTestHost();
        var id = await SeedClosedAsync(host);

        var result = await new GetStockTransferByIdQueryHandler(host.Context)
            .Handle(new GetStockTransferByIdQuery(id, true), TestContext.Current.CancellationToken);

        var dto = result.Data!;
        Assert.Equal(8 * 5m, dto.ClosedShortValueBase);
        Assert.All(dto.Lines, x => Assert.Equal(5m, x.UnitCostBase));
        Assert.Equal(4 * 5m, dto.Lines.Single(x => x.ProductId == 1).ClosedShortValueBase);
        var receipt = Assert.Single(dto.Receipts);
        Assert.Equal(TransferReceiptStatus.Posted, receipt.Status);
        Assert.Equal(6, Assert.Single(receipt.Lines).Quantity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Search_ShouldMaskCosts_UnlessViewCost(bool canViewCost)
    {
        using var host = new TransfersTestHost();
        await SeedClosedAsync(host);

        var result = await new SearchStockTransfersQueryHandler(host.Context)
            .Handle(new SearchStockTransfersQuery(new SearchStockTransferRequest(), canViewCost), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(canViewCost, dto.ClosedShortValueBase.HasValue);
        Assert.All(dto.Lines, x => Assert.Equal(canViewCost, x.UnitCostBase.HasValue));
        Assert.All(dto.Lines, x => Assert.Equal(canViewCost, x.ClosedShortValueBase.HasValue));
        // List rows carry lines but not receipts.
        Assert.Empty(dto.Receipts);
    }

    [Fact]
    public async Task Search_ShouldFilterByStatus_Locations_AndExactCode()
    {
        using var host = new TransfersTestHost();
        var draft = await TransferSeed.DraftAsync(host);
        await TransferSeed.DispatchedAsync(host);
        var handler = new SearchStockTransfersQueryHandler(host.Context);

        var byStatus = await handler.Handle(
            new SearchStockTransfersQuery(new SearchStockTransferRequest { Status = TransferStatus.Draft }, false),
            TestContext.Current.CancellationToken);
        var byCode = await handler.Handle(
            new SearchStockTransfersQuery(new SearchStockTransferRequest { SearchValue = $" {draft.TransferCode.Value.ToLowerInvariant()} " }, false),
            TestContext.Current.CancellationToken);
        var tooLong = await handler.Handle(
            new SearchStockTransfersQuery(new SearchStockTransferRequest { SearchValue = new string('x', 50) }, false),
            TestContext.Current.CancellationToken);
        var bySource = await handler.Handle(
            new SearchStockTransfersQuery(new SearchStockTransferRequest { SourceLocationId = TransferBuilder.Source }, false),
            TestContext.Current.CancellationToken);
        var byWrongDestination = await handler.Handle(
            new SearchStockTransfersQuery(new SearchStockTransferRequest { DestinationLocationId = "other" }, false),
            TestContext.Current.CancellationToken);

        Assert.Equal(draft.Id, Assert.Single(byStatus.Data.Records).Id);
        Assert.Equal(draft.Id, Assert.Single(byCode.Data.Records).Id);
        Assert.Empty(tooLong.Data.Records);
        Assert.Equal(2, bySource.Data.Records.Count());
        Assert.Empty(byWrongDestination.Data.Records);
    }
}
