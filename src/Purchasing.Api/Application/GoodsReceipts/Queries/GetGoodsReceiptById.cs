using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;

namespace StarterKit.Purchasing.Api.Application.GoodsReceipts.Queries;

internal sealed record GetGoodsReceiptByIdQuery(long Id) : IQuery<IResult<GoodsReceiptDto>>;

internal class GetGoodsReceiptByIdQueryHandler(PurchasingDbContext context)
    : IQueryHandler<GetGoodsReceiptByIdQuery, IResult<GoodsReceiptDto>>
{
    public async Task<IResult<GoodsReceiptDto>> Handle(
        GetGoodsReceiptByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.GoodsReceipts
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.PurchaseOrder)
            .Where(new GoodsReceiptByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<GoodsReceiptDto>.NotFound($"Goods receipt {request.Id} not found");

        return Result<GoodsReceiptDto>.Success(ToDto(entity));
    }

    /// <summary>Expects <see cref="GoodsReceipt.Lines"/> and <see cref="GoodsReceipt.PurchaseOrder"/> to be loaded.</summary>
    internal static GoodsReceiptDto ToDto(GoodsReceipt entity) => new()
    {
        Id = entity.Id,
        ReceiptNumber = entity.ReceiptNumber.Value,
        PurchaseOrderId = entity.PurchaseOrderId,
        PONumber = entity.PurchaseOrder.PONumber.Value,
        SupplierId = entity.SupplierId,
        SupplierName = entity.SupplierName,
        LocationId = entity.LocationId,
        LocationName = entity.LocationName,
        DeliveryNoteRef = entity.DeliveryNoteRef,
        ReceivedAt = entity.ReceivedAt,
        Status = entity.Status,
        StockPostedAt = entity.StockPostedAt,
        VoidedAt = entity.VoidedAt,
        VoidReason = entity.VoidReason,
        TotalQuantity = entity.TotalQuantity,
        TotalCostBase = entity.TotalCostBase,
        Lines = entity.Lines
            .Select(x => new GoodsReceiptLineDto
            {
                Id = x.Id,
                PurchaseOrderLineId = x.PurchaseOrderLineId,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Sku = x.Sku,
                Quantity = x.Quantity,
                UnitCostBase = x.UnitCostBase,
                LineTotalBase = x.LineTotalBase,
            })
            .ToList(),
    };
}
