using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Services;
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

internal class AddOrderFeeCommandHandler(
    OrdersDbContext context,
    IOrderTypeCache orderTypeCache)
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

        // Passing the fixed expected category is itself the guard against e.g. a Payment-category id
        // being submitted as a fee type — a lookup for the wrong category simply finds nothing.
        var feeType = await orderTypeCache.GetAsync(model.FeeTypeId, OrderTypeCategory.Fee, cancellationToken);

        if (feeType is null || feeType.Status != OrderTypeStatus.Active)
            return Result<long>.NotFound($"Fee type {model.FeeTypeId} not found or is not active");

        entity.AddFee(model.Name, new Money(model.Amount, CurrencyConstants.Default), feeType.Id, feeType.Name);

        // Order.AddFee appends to the in-memory collection; the new fee's Id is only populated by
        // the identity column once this SaveChangesAsync commits.
        var fee = entity.Fees[^1];

        await context.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(fee.Id);
    }
}
