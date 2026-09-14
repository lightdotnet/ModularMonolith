using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record RemoveOrderDiscountCommand(long OrderId) : ICommand<IResult>;

internal sealed class RemoveOrderDiscountCommandValidator : AbstractValidator<RemoveOrderDiscountCommand>
{
    public RemoveOrderDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
    }
}

internal class RemoveOrderDiscountCommandHandler(OrdersDbContext context)
    : ICommandHandler<RemoveOrderDiscountCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveOrderDiscountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        entity.RemoveDiscount();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
