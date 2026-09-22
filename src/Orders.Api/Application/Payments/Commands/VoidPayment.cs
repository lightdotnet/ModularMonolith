using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Payments.Commands;

internal sealed record VoidPaymentCommand(
    long PaymentId,
    VoidPaymentRequest Model) : ICommand<IResult>;

internal sealed class VoidPaymentCommandValidator : AbstractValidator<VoidPaymentCommand>
{
    public VoidPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new VoidPaymentRequestValidator());
    }
}

internal class VoidPaymentCommandHandler(
    OrdersDbContext context,
    IDateTime clock)
    : ICommandHandler<VoidPaymentCommand, IResult>
{
    public async Task<IResult> Handle(
        VoidPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Where(new PaymentByIdSpec(request.PaymentId))
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
            return Result.NotFound($"Payment {request.PaymentId} not found");

        var order = await context.Orders
            .Include(x => x.Lines)
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(payment.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return Result.NotFound($"Order {payment.OrderId} not found");

        payment.Void(request.Model.Reason, clock.UtcNow);

        // payment.IsVoided is only flipped in memory at this point, so it is excluded from the sum
        // by id rather than by its (still stale, from the database's point of view) IsVoided flag.
        var totalPaid = await context.Payments
            .Where(x => x.OrderId == payment.OrderId && !x.IsVoided && x.Id != payment.Id)
            .SumAsync(x => x.Amount.Amount, cancellationToken);

        order.ReconcilePaymentStatus(totalPaid);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
