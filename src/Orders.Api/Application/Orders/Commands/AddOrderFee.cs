using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record AddOrderFeeCommand(
    long OrderId,
    AddOrderFeeRequest Model) : ICommand<IResult<long>>;

internal sealed class AddOrderFeeCommandValidator : AbstractValidator<AddOrderFeeCommand>
{
    public AddOrderFeeCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddOrderFeeRequestValidator());
    }
}

internal class AddOrderFeeCommandHandler(OrdersDbContext context)
    : ICommandHandler<AddOrderFeeCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        AddOrderFeeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<long>.NotFound($"Order {request.OrderId} not found");

        var model = request.Model;

        entity.AddFee(model.Name, new Money(model.Amount, CurrencyConstants.Default), model.Type);

        // Order.AddFee appends to the in-memory collection; the new fee's Id is only populated by
        // the identity column once this SaveChangesAsync commits.
        var fee = entity.Fees[^1];

        await context.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(fee.Id);
    }
}
