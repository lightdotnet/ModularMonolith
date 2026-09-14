using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Payments;

namespace StarterKit.Orders.Api.Application.Payments.Queries;

internal sealed record GetPaymentsByOrderQuery(long OrderId) : IQuery<IReadOnlyList<PaymentDto>>;

internal class GetPaymentsByOrderQueryHandler(OrdersDbContext context)
    : IQueryHandler<GetPaymentsByOrderQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(
        GetPaymentsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await context.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == request.OrderId)
            .OrderByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToList();
    }

    internal static PaymentDto ToDto(Payment entity) => new()
    {
        Id = entity.Id,
        OrderId = entity.OrderId,
        OrderCode = entity.OrderCode,
        Amount = entity.Amount.Amount,
        Currency = entity.Amount.Currency,
        Method = entity.Method,
        PaidAt = entity.PaidAt,
        Reference = entity.Reference,
        RecordedByUserId = entity.RecordedByUserId,
        IsVoided = entity.IsVoided,
        VoidedAt = entity.VoidedAt,
        VoidReason = entity.VoidReason,
    };
}
