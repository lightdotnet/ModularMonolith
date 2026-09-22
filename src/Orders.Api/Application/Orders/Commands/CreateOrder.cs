using Microsoft.Extensions.Logging;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record CreateOrderCommand(CreateOrderRequest Model) : ICommand<IResult<long>>;

internal sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateOrderRequestValidator());
    }
}

/// <summary>
/// Two distinct creation paths, split by whether <see cref="CreateOrderRequest.OrderCode"/> was
/// supplied: a caller-chosen code is pre-checked for uniqueness and never silently swapped out on a
/// collision (same pre-check-then-friendly-conflict shape as
/// <see cref="StarterKit.Organization.Api.Application.Employees.Commands.LinkEmployeeLoginCommandHandler"/>'s
/// unique-user-account check); an omitted code is generated and gets a bounded retry-on-collision
/// loop instead, since a pre-check cannot meaningfully protect against a random draw's own future
/// collision.
/// </summary>
internal class CreateOrderCommandHandler(
    OrdersDbContext context,
    ILocationDirectoryService locationDirectoryService,
    ICurrencyService currencyService,
    IDateTime clock,
    ILogger<CreateOrderCommandHandler> logger)
    : ICommandHandler<CreateOrderCommand, IResult<long>>
{
    private const int MaxGenerateAttempts = 3;

    public async Task<IResult<long>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        if (!await locationDirectoryService.ExistsAsync(model.LocationId, cancellationToken))
            return Result<long>.NotFound($"Location {model.LocationId} not found");

        // Every order is created in the base currency (no client-side currency choice yet); the code is
        // fixed for the life of the order.
        var baseCurrency = await currencyService.GetBaseCurrencyAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(model.OrderCode)
            ? await CreateWithGeneratedCodeAsync(model, baseCurrency.Code, cancellationToken)
            : await CreateWithCallerSuppliedCodeAsync(model, baseCurrency.Code, cancellationToken);
    }

    private async Task<IResult<long>> CreateWithCallerSuppliedCodeAsync(
        CreateOrderRequest model,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var orderCode = new OrderCode(model.OrderCode!);

        var codeTaken = await context.Orders
            .AnyAsync(x => x.OrderCode == orderCode, cancellationToken);

        if (codeTaken)
            return Result<long>.Error($"Order code '{orderCode.Value}' already exists.");

        var entity = Order.Create(
            model.LocationId,
            model.MemberId,
            currencyCode,
            clock.UtcNow,
            orderCode,
            model.ExternalReferenceCode);

        await context.Orders.AddAsync(entity, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Loses the race against a concurrent order created with the same caller-supplied code;
            // the unique index on Order.OrderCode is the source of truth here, the pre-check above is
            // not. No regenerate — a caller-supplied code is never silently swapped out.
            return Result<long>.Error($"Order code '{orderCode.Value}' already exists.");
        }

        return Result<long>.Success(entity.Id);
    }

    private async Task<IResult<long>> CreateWithGeneratedCodeAsync(
        CreateOrderRequest model,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var entity = Order.Create(
            model.LocationId,
            model.MemberId,
            currencyCode,
            clock.UtcNow,
            externalReferenceCode: model.ExternalReferenceCode);

        await context.Orders.AddAsync(entity, cancellationToken);

        for (var attempt = 1; attempt <= MaxGenerateAttempts; attempt++)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return Result<long>.Success(entity.Id);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation() && attempt < MaxGenerateAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Generated OrderCode collided on attempt {Attempt} of {MaxAttempts}; regenerating.",
                    attempt,
                    MaxGenerateAttempts);

                entity.RegenerateOrderCode(clock.UtcNow);
            }
        }

        // Unreachable in practice: the final attempt's SaveChangesAsync above either returns
        // (success) or throws (an uncaught collision on the last attempt — three straight collisions
        // on a random 9-char draw is exceptional, not an expected outcome to swallow into a Result).
        throw new InvalidOperationException("Unreachable: CreateWithGeneratedCodeAsync retry loop exhausted without returning or throwing.");
    }
}
