using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Queries;

internal sealed record GetStockTransferByIdQuery(
    long Id,
    bool CanViewCost) : IQuery<IResult<StockTransferDto>>;

internal class GetStockTransferByIdQueryHandler(TransfersDbContext context)
    : IQueryHandler<GetStockTransferByIdQuery, IResult<StockTransferDto>>
{
    public async Task<IResult<StockTransferDto>> Handle(
        GetStockTransferByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Receipts)
                .ThenInclude(x => x.Lines)
            .AsSplitQuery()
            .Where(new StockTransferByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<StockTransferDto>.NotFound($"Transfer {request.Id} not found");

        return Result<StockTransferDto>.Success(ToDto(entity, request.CanViewCost));
    }

    /// <summary>Cost fields are left null unless <paramref name="canViewCost"/> is set.</summary>
    internal static StockTransferDto ToDto(
        StockTransfer entity,
        bool canViewCost) => new()
    {
        Id = entity.Id,
        TransferCode = entity.TransferCode.Value,
        SourceLocationId = entity.SourceLocationId,
        SourceLocationName = entity.SourceLocationName,
        DestinationLocationId = entity.DestinationLocationId,
        DestinationLocationName = entity.DestinationLocationName,
        Status = entity.Status,
        Note = entity.Note,
        RequestedAt = entity.RequestedAt,
        DispatchedAt = entity.DispatchedAt,
        ReceivedAt = entity.ReceivedAt,
        ClosedAt = entity.ClosedAt,
        ClosedReason = entity.ClosedReason,
        CancelledAt = entity.CancelledAt,
        CancelledReason = entity.CancelledReason,
        TotalRequestedQuantity = entity.TotalRequestedQuantity,
        TotalInTransitQuantity = entity.TotalInTransitQuantity,
        ClosedShortValueBase = canViewCost ? entity.ClosedShortValueBase : null,
        Lines = entity.Lines
            .Select(x => new TransferLineDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Sku = x.Sku,
                RequestedQuantity = x.RequestedQuantity,
                QtyDispatched = x.QtyDispatched,
                QtyReceived = x.QtyReceived,
                QtyClosedShort = x.QtyClosedShort,
                QtyInTransit = x.QtyInTransit,
                UnitCostBase = canViewCost ? x.UnitCostBase : null,
                ClosedShortValueBase = canViewCost ? x.ClosedShortValueBase : null,
            })
            .ToList(),
        Receipts = entity.Receipts
            .Select(x => new TransferReceiptDto
            {
                Id = x.Id,
                ClientRequestId = x.ClientRequestId,
                Status = x.Status,
                ReceivedAt = x.ReceivedAt,
                VoidedAt = x.VoidedAt,
                VoidReason = x.VoidReason,
                Lines = x.Lines
                    .Select(l => new TransferReceiptLineDto
                    {
                        Id = l.Id,
                        TransferLineId = l.TransferLineId,
                        Quantity = l.Quantity,
                    })
                    .ToList(),
            })
            .ToList(),
    };
}
