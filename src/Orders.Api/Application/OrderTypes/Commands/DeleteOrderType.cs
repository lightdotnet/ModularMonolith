using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;

namespace StarterKit.Orders.Api.Application.OrderTypes.Commands;

internal sealed record DeleteOrderTypeCommand(
    string Id,
    OrderTypeCategory Category) : ICommand<IResult>;

internal sealed class DeleteOrderTypeCommandValidator : AbstractValidator<DeleteOrderTypeCommand>
{
    public DeleteOrderTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
    }
}

internal class DeleteOrderTypeCommandHandler(
    OrdersDbContext context,
    IOrderTypeCache orderTypeCache)
    : ICommandHandler<DeleteOrderTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteOrderTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.OrderTypes
            .Where(new OrderTypeByIdSpec(request.Id, request.Category))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order type {request.Id} ({request.Category}) not found");

        // Unconditional delete, mirroring DeleteFeeType/DeletePaymentType today — OrderFee/Payment
        // keep their own denormalized *TypeName snapshot, so deleting a catalog row does not orphan
        // any historical reference.
        context.OrderTypes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        await orderTypeCache.ReloadAsync(cancellationToken);

        return Result.Success();
    }
}
