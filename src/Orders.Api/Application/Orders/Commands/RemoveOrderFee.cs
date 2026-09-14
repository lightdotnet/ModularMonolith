using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record RemoveOrderFeeCommand(
    long OrderId,
    long OrderFeeId) : ICommand<IResult>;

internal sealed class RemoveOrderFeeCommandValidator : AbstractValidator<RemoveOrderFeeCommand>
{
    public RemoveOrderFeeCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.OrderFeeId).GreaterThan(0);
    }
}

internal class RemoveOrderFeeCommandHandler(OrdersDbContext context)
    : ICommandHandler<RemoveOrderFeeCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveOrderFeeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        entity.RemoveFee(request.OrderFeeId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
