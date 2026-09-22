using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record UpdateOrderLineQuantityCommand(
    long OrderId,
    long OrderLineId,
    UpdateOrderLineQuantityRequest Model) : ICommand<IResult>;

internal sealed class UpdateOrderLineQuantityCommandValidator : AbstractValidator<UpdateOrderLineQuantityCommand>
{
    public UpdateOrderLineQuantityCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.OrderLineId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdateOrderLineQuantityRequestValidator());
    }
}

internal class UpdateOrderLineQuantityCommandHandler(OrdersDbContext context)
    : ICommandHandler<UpdateOrderLineQuantityCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateOrderLineQuantityCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        entity.UpdateLineQuantity(request.OrderLineId, request.Model.Quantity);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
