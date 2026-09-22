using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Api.Services;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record CreateStockTransferCommand(CreateStockTransferRequest Model) : ICommand<IResult<long>>;

internal sealed class CreateStockTransferCommandValidator : AbstractValidator<CreateStockTransferCommand>
{
    public CreateStockTransferCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateStockTransferRequestValidator());
    }
}

/// <summary>
/// The transfer code is always generated, so a unique-index collision is retried with a fresh code
/// (bounded), same shape as <c>CreateOrderCommandHandler</c>'s generated-code path.
/// </summary>
internal class CreateStockTransferCommandHandler(
    TransfersDbContext context,
    TransferLocationResolver locationResolver,
    IDateTime clock,
    ILogger<CreateStockTransferCommandHandler> logger)
    : ICommandHandler<CreateStockTransferCommand, IResult<long>>
{
    private const int MaxGenerateAttempts = 3;

    public async Task<IResult<long>> Handle(
        CreateStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var locations = await locationResolver.ResolveAsync(
            model.SourceLocationId,
            model.DestinationLocationId,
            cancellationToken);

        if (locations.Failed)
            return locations.ToFailure<long>();

        var entity = StockTransfer.Create(
            locations.Source!.Id,
            locations.Source.Name,
            locations.Destination!.Id,
            locations.Destination.Name,
            model.Note,
            clock.UtcNow);

        await context.StockTransfers.AddAsync(entity, cancellationToken);

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
                    "Generated TransferCode collided on attempt {Attempt} of {MaxAttempts}; regenerating.",
                    attempt,
                    MaxGenerateAttempts);

                entity.RegenerateTransferCode(clock.UtcNow);
            }
        }

        // Unreachable in practice: the final attempt either returns (success) or throws.
        throw new InvalidOperationException("Unreachable: CreateStockTransferCommandHandler retry loop exhausted without returning or throwing.");
    }
}
