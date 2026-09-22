using Light.Exceptions;
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

        ApprovalRequest entity;

        try
        {
            entity = ApprovalRequest.Create(
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
        }
        catch (ExceptionBase ex)
        {
            return ToStringResult(ex);
        }

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

            try
            {
                entity.Decide(decidedByUserId, approved, comment, clock.AuditTime);
            }
            catch (ExceptionBase ex)
            {
                return ToResult(ex);
            }

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

            try
            {
                entity.Cancel(cancelledByUserId, clock.AuditTime);
            }
            catch (ExceptionBase ex)
            {
                return ToResult(ex);
            }

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

    public Task<ApprovalStatusView?> GetStatusAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        return context.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.Id == approvalRequestId)
            .Select(x => new ApprovalStatusView(
                x.Id,
                x.RequestType,
                x.RequestId,
                x.Status,
                x.CurrentLevel))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, ApprovalStatusView>> GetStatusesAsync(
        IReadOnlyCollection<string> approvalRequestIds,
        CancellationToken cancellationToken = default)
    {
        if (approvalRequestIds is null || approvalRequestIds.Count == 0)
            return new Dictionary<string, ApprovalStatusView>();

        var ids = approvalRequestIds.Distinct().ToList();

        var rows = await context.ApprovalRequests
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new ApprovalStatusView(
                x.Id,
                x.RequestType,
                x.RequestId,
                x.Status,
                x.CurrentLevel))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.ApprovalRequestId);
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

    private static IResult ToResult(ExceptionBase ex) => ex switch
    {
        ConflictException => Result.Conflict(ex.Message),
        ForbiddenException => Result.Forbidden(ex.Message),
        _ => Result.Error(DescribeValidation(ex)),
    };

    private static IResult<string> ToStringResult(ExceptionBase ex) => ex switch
    {
        ConflictException => Result<string>.Conflict(ex.Message),
        ForbiddenException => Result<string>.Forbidden(ex.Message),
        _ => Result<string>.Error(DescribeValidation(ex)),
    };

    private static string DescribeValidation(ExceptionBase ex) =>
        ex is ValidationException v && v.ValidationErrors.Count > 0
            ? string.Join("|", v.ValidationErrors.Select(e => $"{e.Key}: {string.Join(",", e.Value)}"))
            : ex.Message;

    private async Task PublishFinalizedIntegrationEventAsync(
        ApprovalRequest entity,
        CancellationToken cancellationToken)
    {
        // A requester-initiated cancellation is always driven by the owning module, which learns the
        // outcome directly from CancelAsync's result — publishing here would only re-enter that same
        // module's scope mid-command. A future non-requester cancel path (e.g. an admin force-cancel)
        // must publish its own finalized event explicitly.
        if (entity.Status is ApprovalStatus.Pending or ApprovalStatus.Cancelled)
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
