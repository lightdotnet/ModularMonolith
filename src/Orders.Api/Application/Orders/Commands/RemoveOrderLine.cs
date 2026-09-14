using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record RemoveOrderLineCommand(
    long OrderId,
    long OrderLineId) : ICommand<IResult>;

internal sealed class RemoveOrderLineCommandValidator : AbstractValidator<RemoveOrderLineCommand>
{
    public RemoveOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.OrderLineId).GreaterThan(0);
    }
}

internal class RemoveOrderLineCommandHandler(OrdersDbContext context)
    : ICommandHandler<RemoveOrderLineCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        entity.RemoveLine(request.OrderLineId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
