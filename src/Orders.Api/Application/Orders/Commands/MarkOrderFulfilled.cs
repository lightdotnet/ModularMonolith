using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record MarkOrderFulfilledCommand(long Id) : ICommand<IResult>;

internal sealed class MarkOrderFulfilledCommandValidator : AbstractValidator<MarkOrderFulfilledCommand>
{
    public MarkOrderFulfilledCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class MarkOrderFulfilledCommandHandler(
    OrdersDbContext context,
    IDateTime clock)
    : ICommandHandler<MarkOrderFulfilledCommand, IResult>
{
    public async Task<IResult> Handle(
        MarkOrderFulfilledCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Where(new OrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.Id} not found");

        entity.MarkFulfilled(clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
