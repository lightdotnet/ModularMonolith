using Mapster;
using Microsoft.Extensions.Logging;
using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Shared;

namespace StarterKit.Approval.Api.Services;

internal class ApprovalService(
    ApprovalDbContext context,
    IPublisher publisher,
    IDateTime clock,
    ILogger<ApprovalService> logger) : IApprovalService
{
    private const string ConcurrencyConflictMessage =
        "This approval request was just updated by someone else. Reload and try again.";

    public async Task<IResult<string>> CreateAsync(
        CreateApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DocumentTypeId is not null
            && !await context.ApprovalDocumentTypes
                .AnyAsync(x => x.Id == request.DocumentTypeId && x.IsActive, cancellationToken))
        {
            return Result<string>.Error(
                $"Approval document type {request.DocumentTypeId} was not found or is not active.");
        }

        var creation = ApprovalRequest.Create(
            request.RequestType,
            request.RequestId,
            request.RequesterUserId,
            request.RequesterEmployeeId,
            request.RequesterName,
            request.Title,
            request.Content,
            request.DeepLinkUrl,
            request.DocumentTypeId,
            request.ApproverChain);

        if (!creation.IsSuccess)
            return Result<string>.Error(creation.Message);

        var entity = creation.Data;

        await context.ApprovalRequests.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }

    public async Task<IResult> DecideAsync(
        string approvalRequestId,
        string decidedByUserId,
        bool approved,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            var entity = await LoadWithStepsAsync(approvalRequestId, cancellationToken);

            if (entity is null)
                return Result.NotFound($"Approval request {approvalRequestId} not found");

            var decision = entity.Decide(decidedByUserId, approved, comment, clock.AuditTime);

            if (!decision.IsSuccess)
                return decision;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt >= 1)
                    return Result.Conflict(ConcurrencyConflictMessage);

                Detach(entity);
                continue;
            }

            await PublishFinalizedIntegrationEventAsync(entity, cancellationToken);

            return Result.Success();
        }
    }

    public async Task<IResult> CancelAsync(
        string approvalRequestId,
        string cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            var entity = await LoadWithStepsAsync(approvalRequestId, cancellationToken);

            if (entity is null)
                return Result.NotFound($"Approval request {approvalRequestId} not found");

            var cancellation = entity.Cancel(cancelledByUserId, clock.AuditTime);

            if (!cancellation.IsSuccess)
                return cancellation;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt >= 1)
                    return Result.Conflict(ConcurrencyConflictMessage);

                Detach(entity);
                continue;
            }

            await PublishFinalizedIntegrationEventAsync(entity, cancellationToken);

            return Result.Success();
        }
    }

    public Task<ApprovalRequestDto?> GetByRequestAsync(
        string requestType,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        // (RequestType, RequestId) is an opaque reference to an object owned by the calling module,
        // not a unique key here - a caller can legitimately raise a new request for the same object
        // after an earlier one was rejected or cancelled. Return the most recent match rather than
        // throwing when more than one exists.
        return context.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.RequestType == requestType && x.RequestId == requestId)
            .OrderByDescending(x => x.Created)
            .ProjectToType<ApprovalRequestDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ApprovalStatusView?> GetStatusByRequestAsync(
        string requestType,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        return context.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.RequestType == requestType && x.RequestId == requestId)
            .OrderByDescending(x => x.Created)
            .Select(x => new ApprovalStatusView(
                x.Id,
                x.RequestType,
                x.RequestId,
                x.Status,
                x.CurrentLevel))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, ApprovalStatusView>> GetStatusesByRequestAsync(
        string requestType,
        IReadOnlyCollection<string> requestIds,
        CancellationToken cancellationToken = default)
    {
        if (requestIds is null || requestIds.Count == 0)
            return new Dictionary<string, ApprovalStatusView>();

        var ids = requestIds.Distinct().ToList();

        var rows = await context.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.RequestType == requestType && ids.Contains(x.RequestId))
            .OrderByDescending(x => x.Created)
            .Select(x => new ApprovalStatusView(
                x.Id,
                x.RequestType,
                x.RequestId,
                x.Status,
                x.CurrentLevel))
            .ToListAsync(cancellationToken);

        // Rows arrive newest-first; GroupBy preserves source order, so First() per group is the
        // most recent approval request for that source record.
        return rows
            .GroupBy(x => x.RequestId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private Task<ApprovalRequest?> LoadWithStepsAsync(
        string approvalRequestId,
        CancellationToken cancellationToken) =>
        context.ApprovalRequests
            .Include(x => x.Steps)
            .Where(new ApprovalRequestByIdSpec(approvalRequestId))
            .FirstOrDefaultAsync(cancellationToken);

    private void Detach(ApprovalRequest entity)
    {
        foreach (var step in entity.Steps)
            context.Entry(step).State = EntityState.Detached;

        context.Entry(entity).State = EntityState.Detached;
    }

    private async Task PublishFinalizedIntegrationEventAsync(
        ApprovalRequest entity,
        CancellationToken cancellationToken)
    {
        if (entity.Status == ApprovalStatus.Pending)
            return;

        // A downstream subscriber fault must never fail the decide/cancel call. The periodic
        // reconciliation backstop in the owning module covers a dropped delivery.
        try
        {
            await publisher.Publish(
                new ApprovalFinalizedIntegrationEvent(
                    entity.RequestType,
                    entity.RequestId,
                    entity.Id,
                    entity.Status),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {IntegrationEvent} for approval request {ApprovalRequestId}.",
                nameof(ApprovalFinalizedIntegrationEvent),
                entity.Id);
        }
    }
}
