using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;

namespace StarterKit.Orders.Api.Application.OrderTypes.Commands;

internal sealed record CreateOrderTypeCommand(CreateOrderTypeRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateOrderTypeCommandValidator : AbstractValidator<CreateOrderTypeCommand>
{
    public CreateOrderTypeCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateOrderTypeRequestValidator());
    }
}

internal class CreateOrderTypeCommandHandler(
    OrdersDbContext context,
    IOrderTypeCache orderTypeCache)
    : ICommandHandler<CreateOrderTypeCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateOrderTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var idExists = await context.OrderTypes
            .AnyAsync(x => x.Id == model.Id && x.Category == model.Category, cancellationToken);

        if (idExists)
            return Result<string>.Error($"Order type '{model.Id}' already exists for category '{model.Category}'.");

        var entity = OrderType.Create(model.Id, model.Category, model.Name);

        await context.OrderTypes.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await orderTypeCache.ReloadAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
