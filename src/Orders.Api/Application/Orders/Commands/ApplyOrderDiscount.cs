using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record ApplyOrderDiscountCommand(
    long OrderId,
    ApplyOrderDiscountRequest Model) : ICommand<IResult>;

internal sealed class ApplyOrderDiscountCommandValidator : AbstractValidator<ApplyOrderDiscountCommand>
{
    public ApplyOrderDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new ApplyOrderDiscountRequestValidator());
    }
}

internal class ApplyOrderDiscountCommandHandler(OrdersDbContext context)
    : ICommandHandler<ApplyOrderDiscountCommand, IResult>
{
    public async Task<IResult> Handle(
        ApplyOrderDiscountCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        entity.ApplyDiscount(request.Model.Kind, request.Model.Value);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
