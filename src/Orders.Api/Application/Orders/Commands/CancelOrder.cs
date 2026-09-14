using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record CancelOrderCommand(
    long Id,
    CancelOrderRequest Model,
    string CancelledByUserId) : ICommand<IResult>;

internal sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CancelledByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new CancelOrderRequestValidator());
    }
}

internal class CancelOrderCommandHandler(
    OrdersDbContext context,
    IDateTime clock)
    : ICommandHandler<CancelOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Where(new OrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.Id} not found");

        entity.Cancel(request.CancelledByUserId, request.Model.Reason, clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
