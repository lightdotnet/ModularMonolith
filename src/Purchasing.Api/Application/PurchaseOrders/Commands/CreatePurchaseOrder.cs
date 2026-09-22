using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Services;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record CreatePurchaseOrderCommand(
    CreatePurchaseOrderRequest Model,
    string RequesterUserId,
    string? RequesterEmployeeId) : ICommand<IResult<long>>;

internal sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator(IDateTime clock)
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();

        // Sanity only: an expected date long before now is almost certainly a typo.
        RuleFor(x => x.Model.ExpectedAt)
            .GreaterThanOrEqualTo(_ => clock.UtcNow.AddDays(-1))
            .When(x => x.Model.ExpectedAt.HasValue)
            .WithMessage("The expected date cannot be in the past.");
        RuleFor(x => x.Model).SetValidator(new CreatePurchaseOrderRequestValidator());
    }
}

/// <summary>
/// The requester is the caller (resolved from their employee link — the same employee later picks an
/// approver from their own org chain). The supplier must be active and the receiving location active.
/// The PO number is always generated, so a unique-index collision is retried with a fresh number
/// (bounded), same shape as <c>CreateStockTransferCommandHandler</c>.
/// </summary>
internal class CreatePurchaseOrderCommandHandler(
    PurchasingDbContext context,
    ReceivingLocationResolver locationResolver,
    IDateTime clock,
    ILogger<CreatePurchaseOrderCommandHandler> logger)
    : ICommandHandler<CreatePurchaseOrderCommand, IResult<long>>
{
    private const int MaxGenerateAttempts = 3;

    public async Task<IResult<long>> Handle(
        CreatePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RequesterEmployeeId))
            return Result<long>.Error("Your account is not linked to an employee record.");

        var model = request.Model;

        var supplier = await context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.SupplierId, cancellationToken);

        if (supplier is null)
            return Result<long>.NotFound($"Supplier {model.SupplierId} not found");

        if (!supplier.IsActive)
            return Result<long>.Error($"Supplier {supplier.Name} is not active");

        var location = await locationResolver.ResolveAsync(model.LocationId, cancellationToken);

        if (location.Failed)
            return location.ToFailure<long>();

        var entity = PurchaseOrder.Create(
            supplier.Id,
            supplier.Name,
            location.Location!.Id,
            location.Location.Name,
            model.ExpectedAt,
            request.RequesterUserId,
            request.RequesterEmployeeId,
            model.Note,
            clock.UtcNow);

        await context.PurchaseOrders.AddAsync(entity, cancellationToken);

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
                    "Generated PO number collided on attempt {Attempt} of {MaxAttempts}; regenerating.",
                    attempt,
                    MaxGenerateAttempts);

                entity.RegeneratePONumber(clock.UtcNow);
            }
        }

        // Unreachable in practice: the final attempt either returns (success) or throws.
        throw new InvalidOperationException("Unreachable: CreatePurchaseOrderCommandHandler retry loop exhausted without returning or throwing.");
    }
}
