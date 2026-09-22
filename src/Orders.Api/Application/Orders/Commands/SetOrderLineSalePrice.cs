using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record SetOrderLineSalePriceCommand(
    long OrderId,
    long OrderLineId,
    SetOrderLineSalePriceRequest Model) : ICommand<IResult>;

internal sealed class SetOrderLineSalePriceCommandValidator : AbstractValidator<SetOrderLineSalePriceCommand>
{
    public SetOrderLineSalePriceCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.OrderLineId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new SetOrderLineSalePriceRequestValidator());
    }
}

internal class SetOrderLineSalePriceCommandHandler(OrdersDbContext context)
    : ICommandHandler<SetOrderLineSalePriceCommand, IResult>
{
    public async Task<IResult> Handle(
        SetOrderLineSalePriceCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        var salePrice = request.Model.SalePrice.HasValue
            ? new Money(request.Model.SalePrice.Value, entity.CurrencyCode)
            : null;

        entity.SetLineSalePrice(request.OrderLineId, salePrice);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
