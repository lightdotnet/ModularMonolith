using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;

namespace StarterKit.Orders.Api.Application.OrderTypes.Commands;

internal sealed record UpdateOrderTypeCommand(
    string Id,
    OrderTypeCategory Category,
    UpdateOrderTypeRequest Model) : ICommand<IResult>;

internal sealed class UpdateOrderTypeCommandValidator : AbstractValidator<UpdateOrderTypeCommand>
{
    public UpdateOrderTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Model).SetValidator(new UpdateOrderTypeRequestValidator());
    }
}

internal class UpdateOrderTypeCommandHandler(
    OrdersDbContext context,
    IOrderTypeCache orderTypeCache)
    : ICommandHandler<UpdateOrderTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateOrderTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.OrderTypes
            .Where(new OrderTypeByIdSpec(request.Id, request.Category))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order type {request.Id} ({request.Category}) not found");

        entity.Update(model.Name, model.Status);

        await context.SaveChangesAsync(cancellationToken);

        await orderTypeCache.ReloadAsync(cancellationToken);

        return Result.Success();
    }
}
