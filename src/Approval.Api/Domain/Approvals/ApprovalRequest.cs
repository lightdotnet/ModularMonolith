using StarterKit.Shared.Entities;

namespace StarterKit.Approval.Api.Domain.Approvals;

public class ApprovalRequest : AuditableEntity
{
    private readonly List<ApprovalStep> _steps = [];

    private ApprovalRequest()
    {
    }

    public string RequestType { get; private set; } = null!;

    public string RequestId { get; private set; } = null!;

    public string RequesterUserId { get; private set; } = null!;

    /// <summary>
    /// Opaque bookkeeping reference to the requester's Organization employee record.
    /// Null when the requester's account is not linked to an employee.
    /// </summary>
    public string? RequesterEmployeeId { get; private set; }

    /// <summary>
    /// Display label for the requester, captured at creation time by the calling module.
    /// Approval has no view onto identity/organization data, so it cannot resolve this itself.
    /// </summary>
    public string? RequesterName { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Content { get; private set; }

    public string? DeepLinkUrl { get; private set; }

    public string? DocumentTypeId { get; private set; }

    public ApprovalDocumentType? DocumentType { get; private set; }

    public int CurrentLevel { get; private set; }

    public ApprovalStatus Status { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>ApprovalDbContext</c> on every
    /// update. A stale token makes a concurrent decide/cancel fail with
    /// <c>DbUpdateConcurrencyException</c> rather than silently clobbering the other decision.
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public IReadOnlyList<ApprovalStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Builds a new approval request, enforcing every chain invariant. Returns a failed
    /// <see cref="Result{T}"/> rather than throwing so the caller surfaces a clean error.
    /// </summary>
    public static IResult<ApprovalRequest> Create(
        string requestType,
        string requestId,
        string requesterUserId,
        string? requesterEmployeeId,
        string? requesterName,
        string title,
        string? content,
        string? deepLinkUrl,
        string? documentTypeId,
        IReadOnlyList<ApproverStepInput> approverChain)
    {
        if (approverChain is null || approverChain.Count == 0)
            return Result<ApprovalRequest>.Error("At least one approver level is required.");

        if (approverChain.Any(x => x.Level < 1))
            return Result<ApprovalRequest>.Error("Approver chain levels must be positive.");

        if (approverChain.Select(x => x.Level).Distinct().Count() != approverChain.Count)
            return Result<ApprovalRequest>.Error("Approver chain levels must be unique.");

        if (approverChain.Any(x =>
                string.IsNullOrWhiteSpace(x.ApproverUserId)
                || string.IsNullOrWhiteSpace(x.ApproverEmployeeId)))
        {
            return Result<ApprovalRequest>.Error("Every approver step requires an approver user and employee id.");
        }

        if (string.IsNullOrWhiteSpace(requesterUserId))
            return Result<ApprovalRequest>.Error("A requester is required.");

        if (string.IsNullOrWhiteSpace(title))
            return Result<ApprovalRequest>.Error("A title is required.");

        if (string.IsNullOrWhiteSpace(requestType))
            return Result<ApprovalRequest>.Error("A request type is required.");

        if (string.IsNullOrWhiteSpace(requestId))
            return Result<ApprovalRequest>.Error("A request id is required.");

        var entity = new ApprovalRequest
        {
            RequestType = requestType,
            RequestId = requestId,
            RequesterUserId = requesterUserId,
            RequesterEmployeeId = requesterEmployeeId,
            RequesterName = requesterName,
            Title = title,
            Content = content,
            DeepLinkUrl = deepLinkUrl,
            DocumentTypeId = documentTypeId,
            Status = ApprovalStatus.Pending,
        };

        foreach (var step in approverChain.OrderBy(x => x.Level))
        {
            entity._steps.Add(
                ApprovalStep.Create(
                    step.Level,
                    step.ApproverUserId,
                    step.ApproverEmployeeId,
                    step.ApproverName));
        }

        entity.CurrentLevel = entity._steps.Min(x => x.Level);

        var firstStep = entity._steps.First(x => x.Level == entity.CurrentLevel);

        entity.AddDomainEvent(
            new ApprovalStepPendingEvent(
                entity.Id,
                entity.Title,
                entity.DeepLinkUrl,
                firstStep.ApproverUserId,
                entity.RequesterUserId));

        return Result<ApprovalRequest>.Success(entity);
    }

    /// <summary>
    /// Records the current level's decision. Advances to the next pending level on a non-final
    /// approval, otherwise finalizes the request. Never throws — guard failures come back as a
    /// failed <see cref="Result"/>.
    /// </summary>
    public IResult Decide(
        string decidedByUserId,
        bool approved,
        string? comment,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(decidedByUserId))
            return Result.Error("A decider is required.");

        if (Status != ApprovalStatus.Pending)
            return Result.Error("This approval request has already been finalized.");

        var currentStep = _steps
            .Where(x => x.Level == CurrentLevel)
            .OrderBy(x => x.Level)
            .FirstOrDefault();

        if (currentStep is null)
            return Result.Error("Current approval step could not be resolved.");

        if (!currentStep.IsPending)
            return Result.Error("The current approval step is no longer pending.");

        if (currentStep.ApproverUserId != decidedByUserId)
            return Result.Error("You are not the assigned approver for this step.");

        if (!approved && string.IsNullOrWhiteSpace(comment))
            return Result.Error("A reason is required when rejecting a request.");

        if (!approved)
        {
            currentStep.Reject(comment, decidedAt);
            Status = ApprovalStatus.Rejected;
            FinalizedAt = decidedAt;

            SkipPendingSteps();

            AddDomainEvent(
                new ApprovalFinalizedEvent(
                    Id,
                    Title,
                    DeepLinkUrl,
                    RequesterUserId,
                    decidedByUserId,
                    Status));

            return Result.Success();
        }

        currentStep.Approve(comment, decidedAt);

        var nextStep = _steps
            .Where(x => x.Level > CurrentLevel && x.IsPending)
            .OrderBy(x => x.Level)
            .FirstOrDefault();

        if (nextStep is null)
        {
            Status = ApprovalStatus.Approved;
            FinalizedAt = decidedAt;

            SkipPendingSteps();

            AddDomainEvent(
                new ApprovalFinalizedEvent(
                    Id,
                    Title,
                    DeepLinkUrl,
                    RequesterUserId,
                    decidedByUserId,
                    Status));

            return Result.Success();
        }

        CurrentLevel = nextStep.Level;

        AddDomainEvent(
            new ApprovalStepPendingEvent(
                Id,
                Title,
                DeepLinkUrl,
                nextStep.ApproverUserId,
                RequesterUserId));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws a still-pending request. Only the original requester may cancel — approvers
    /// reject, they do not cancel.
    /// </summary>
    public IResult Cancel(
        string cancelledByUserId,
        DateTimeOffset cancelledAt)
    {
        if (string.IsNullOrWhiteSpace(cancelledByUserId))
            return Result.Error("A canceller is required.");

        if (Status != ApprovalStatus.Pending)
            return Result.Error("Only a pending approval request can be cancelled.");

        if (cancelledByUserId != RequesterUserId)
            return Result.Error("Only the requester may cancel this approval request.");

        var currentApproverUserId = _steps
            .FirstOrDefault(x => x.Level == CurrentLevel && x.IsPending)?
            .ApproverUserId;

        Status = ApprovalStatus.Cancelled;
        FinalizedAt = cancelledAt;

        SkipPendingSteps();

        AddDomainEvent(
            new ApprovalRequestCancelledEvent(
                Id,
                Title,
                DeepLinkUrl,
                RequesterUserId,
                cancelledByUserId,
                currentApproverUserId));

        return Result.Success();
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>ApprovalDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    private void SkipPendingSteps()
    {
        foreach (var step in _steps.Where(x => x.IsPending))
            step.Skip();
    }
}
