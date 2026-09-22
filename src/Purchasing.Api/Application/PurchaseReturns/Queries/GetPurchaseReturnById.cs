using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Queries;

internal sealed record GetPurchaseReturnByIdQuery(
    long Id,
    bool CanViewStockCost) : IQuery<IResult<PurchaseReturnDto>>;

internal class GetPurchaseReturnByIdQueryHandler(PurchasingDbContext context)
    : IQueryHandler<GetPurchaseReturnByIdQuery, IResult<PurchaseReturnDto>>
{
    public async Task<IResult<PurchaseReturnDto>> Handle(
        GetPurchaseReturnByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseReturns
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.GoodsReceipt)
            .Where(new PurchaseReturnByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<PurchaseReturnDto>.NotFound($"Purchase return {request.Id} not found");

        return Result<PurchaseReturnDto>.Success(ToDto(entity, request.CanViewStockCost));
    }

    /// <summary>Expects <see cref="PurchaseReturn.Lines"/> and <see cref="PurchaseReturn.GoodsReceipt"/> to be loaded.</summary>
    /// <summary>
    /// The single mapping used by get and search. The cost Inventory removed from stock is its moving-average
    /// cost and is left null unless <paramref name="canViewStockCost"/> is set; Purchasing own purchase prices
    /// (receipt unit cost, expected credit) are always returned.
    /// </summary>
    internal static PurchaseReturnDto ToDto(
        PurchaseReturn entity,
        bool canViewStockCost) => new()
    {
        Id = entity.Id,
        ReturnNumber = entity.ReturnNumber.Value,
        SupplierId = entity.SupplierId,
        SupplierName = entity.SupplierName,
        GoodsReceiptId = entity.GoodsReceiptId,
        ReceiptNumber = entity.GoodsReceipt.ReceiptNumber.Value,
        PurchaseOrderId = entity.PurchaseOrderId,
        LocationId = entity.LocationId,
        LocationName = entity.LocationName,
        Reason = entity.Reason,
        Note = entity.Note,
        Status = entity.Status,
        PostedAt = entity.PostedAt,
        Created = entity.Created,
        TotalQuantity = entity.TotalQuantity,
        ExpectedCreditBase = entity.ExpectedCreditBase,
        CostRemovedBase = canViewStockCost ? entity.CostRemovedBase : null,
        CreditNoteNumber = entity.CreditNoteNumber,
        CreditAmountBase = entity.CreditAmountBase,
        CreditedAt = entity.CreditedAt,
        CancelledAt = entity.CancelledAt,
        CancelledReason = entity.CancelledReason,
        Lines = entity.Lines
            .Select(x => new PurchaseReturnLineDto
            {
                Id = x.Id,
                GoodsReceiptLineId = x.GoodsReceiptLineId,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Sku = x.Sku,
                Quantity = x.Quantity,
                ReceiptUnitCostBase = x.ReceiptUnitCostBase,
                CostRemovedBase = canViewStockCost ? x.CostRemovedBase : null,
                Reason = x.Reason,
            })
            .ToList(),
    };
}
