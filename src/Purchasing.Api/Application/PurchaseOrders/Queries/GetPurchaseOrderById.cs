using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Queries;

internal sealed record GetPurchaseOrderByIdQuery(long Id) : IQuery<IResult<PurchaseOrderDto>>;

internal class GetPurchaseOrderByIdQueryHandler(PurchasingDbContext context)
    : IQueryHandler<GetPurchaseOrderByIdQuery, IResult<PurchaseOrderDto>>
{
    public async Task<IResult<PurchaseOrderDto>> Handle(
        GetPurchaseOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<PurchaseOrderDto>.NotFound($"Purchase order {request.Id} not found");

        var dto = ToDto(entity);

        // Materialize before mapping: the receipt number is a converted value object, so its inner
        // value is read client-side.
        var receipts = await context.GoodsReceipts
            .AsNoTracking()
            .Where(x => x.PurchaseOrderId == request.Id)
            .OrderBy(x => x.Created)
            .Select(x => new
            {
                x.Id,
                x.ReceiptNumber,
                x.Status,
                x.DeliveryNoteRef,
                x.ReceivedAt,
                TotalQuantity = x.Lines.Sum(l => l.Quantity),
            })
            .ToListAsync(cancellationToken);

        dto.Receipts = receipts
            .Select(x => new PurchaseOrderReceiptSummaryDto
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber.Value,
                Status = x.Status,
                DeliveryNoteRef = x.DeliveryNoteRef,
                ReceivedAt = x.ReceivedAt,
                TotalQuantity = x.TotalQuantity,
            })
            .ToList();

        return Result<PurchaseOrderDto>.Success(dto);
    }

    internal static PurchaseOrderDto ToDto(PurchaseOrder entity)
    {
        var total = entity.TotalAmount;

        return new PurchaseOrderDto
        {
            Id = entity.Id,
            PONumber = entity.PONumber.Value,
            SupplierId = entity.SupplierId,
            SupplierName = entity.SupplierName,
            LocationId = entity.LocationId,
            LocationName = entity.LocationName,
            ExpectedAt = entity.ExpectedAt,
            Status = entity.Status,
            Note = entity.Note,
            RequesterEmployeeId = entity.RequesterEmployeeId,
            ApprovalRequestId = entity.ApprovalRequestId,
            ApproverEmployeeId = entity.ApproverEmployeeId,
            ApproverName = entity.ApproverName,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            RejectedAt = entity.RejectedAt,
            ReceivedAt = entity.ReceivedAt,
            ClosedAt = entity.ClosedAt,
            ClosedReason = entity.ClosedReason,
            CancelledAt = entity.CancelledAt,
            CancelledReason = entity.CancelledReason,
            Created = entity.Created,
            Currency = total.Currency,
            TotalAmount = total.Amount,
            TotalOrderedQuantity = entity.TotalOrderedQuantity,
            TotalReceivedQuantity = entity.TotalReceivedQuantity,
            TotalOutstandingQuantity = entity.TotalOutstandingQuantity,
            Lines = entity.Lines
                .Select(x => new PurchaseOrderLineDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Sku = x.Sku,
                    OrderedQuantity = x.OrderedQuantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    ReturnedQuantity = x.ReturnedQuantity,
                    OutstandingQuantity = x.OutstandingQuantity,
                    UnitCost = x.UnitCost.Amount,
                    LineTotal = x.LineTotal.Amount,
                })
                .ToList(),
        };
    }
}
