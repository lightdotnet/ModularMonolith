using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Orders.Api.Services;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Application.Payments.Commands;

internal sealed record RecordPaymentCommand(
    long OrderId,
    RecordPaymentRequest Model,
    string RecordedByUserId) : ICommand<IResult<long>>;

internal sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.RecordedByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new RecordPaymentRequestValidator());
    }
}

internal class RecordPaymentCommandHandler(
    OrdersDbContext context,
    IOrderTypeCache orderTypeCache)
    : ICommandHandler<RecordPaymentCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        RecordPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .Include(x => x.Lines)
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return Result<long>.NotFound($"Order {request.OrderId} not found");

        var model = request.Model;

        // Passing the fixed expected category is itself the guard against e.g. a Fee-category id
        // being submitted as a payment type — a lookup for the wrong category simply finds nothing.
        var paymentType = await orderTypeCache.GetAsync(model.PaymentTypeId, OrderTypeCategory.Payment, cancellationToken);

        if (paymentType is null || paymentType.Status != OrderTypeStatus.Active)
            return Result<long>.NotFound($"Payment type {model.PaymentTypeId} not found or is not active");

        var payment = Payment.Create(
            request.OrderId,
            order.OrderCode.Value,
            new Money(model.Amount, model.Currency),
            paymentType.Id,
            paymentType.Name,
            model.PaidAt,
            model.Reference,
            request.RecordedByUserId);

        await context.Payments.AddAsync(payment, cancellationToken);

        // The new payment is not in the database yet, so it is added to the already-persisted
        // non-voided total by hand rather than re-querying for it.
        var priorPaid = await context.Payments
            .Where(x => x.OrderId == request.OrderId && !x.IsVoided)
            .SumAsync(x => x.Amount.Amount, cancellationToken);

        order.ReconcilePaymentStatus(priorPaid + model.Amount);

        await context.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(payment.Id);
    }
}
