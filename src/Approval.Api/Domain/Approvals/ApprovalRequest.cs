using Light.Exceptions;
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

    public virtual ApprovalDocumentType? DocumentType { get; private set; }

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
    /// Builds a new approval request, enforcing every chain invariant. Throws a
    /// <see cref="ValidationException"/> when any invariant is violated so the caller can surface a
    /// clean error at the boundary.
    /// </summary>
    public static ApprovalRequest Create(
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
            throw Invalid("approverChain", "At least one approver level is required.");

        if (approverChain.Any(x => x.Level < 1))
            throw Invalid("approverChain", "Approver chain levels must be positive.");

        if (approverChain.Select(x => x.Level).Distinct().Count() != approverChain.Count)
            throw Invalid("approverChain", "Approver chain levels must be unique.");

        if (approverChain.Any(x =>
                string.IsNullOrWhiteSpace(x.ApproverUserId)
                || string.IsNullOrWhiteSpace(x.ApproverEmployeeId)))
        {
            throw Invalid("approverChain", "Every approver step requires an approver user and employee id.");
        }

        if (string.IsNullOrWhiteSpace(requesterUserId))
            throw Invalid("requesterUserId", "A requester is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw Invalid("title", "A title is required.");

        if (string.IsNullOrWhiteSpace(requestType))
            throw Invalid("requestType", "A request type is required.");

        if (string.IsNullOrWhiteSpace(requestId))
            throw Invalid("requestId", "A request id is required.");

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

        return entity;
    }

    /// <summary>
    /// Records the current level's decision. Advances to the next pending level on a non-final
    /// approval, otherwise finalizes the request. Throws <see cref="ValidationException"/>,
    /// <see cref="ConflictException"/>, or <see cref="ForbiddenException"/> when a guard fails.
    /// </summary>
    public void Decide(
        string decidedByUserId,
        bool approved,
        string? comment,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(decidedByUserId))
            throw Invalid("decidedByUserId", "A decider is required.");

        if (Status != ApprovalStatus.Pending)
            throw new ConflictException("This approval request has already been finalized.");

        var currentStep = _steps
            .Where(x => x.Level == CurrentLevel)
            .OrderBy(x => x.Level)
            .FirstOrDefault();

        if (currentStep is null)
            throw new ConflictException("Current approval step could not be resolved.");

        if (!currentStep.IsPending)
            throw new ConflictException("The current approval step is no longer pending.");

        if (currentStep.ApproverUserId != decidedByUserId)
            throw new ForbiddenException("You are not the assigned approver for this step.");

        if (!approved && string.IsNullOrWhiteSpace(comment))
            throw Invalid("comment", "A reason is required when rejecting a request.");

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

            return;
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

            return;
        }

        CurrentLevel = nextStep.Level;

        AddDomainEvent(
            new ApprovalStepPendingEvent(
                Id,
                Title,
                DeepLinkUrl,
                nextStep.ApproverUserId,
                RequesterUserId));
    }

    /// <summary>
    /// Withdraws a still-pending request. Only the original requester may cancel — approvers
    /// reject, they do not cancel. Throws <see cref="ValidationException"/>,
    /// <see cref="ConflictException"/>, or <see cref="ForbiddenException"/> when a guard fails.
    /// </summary>
    public void Cancel(
        string cancelledByUserId,
        DateTimeOffset cancelledAt)
    {
        if (string.IsNullOrWhiteSpace(cancelledByUserId))
            throw Invalid("cancelledByUserId", "A canceller is required.");

        if (Status != ApprovalStatus.Pending)
            throw new ConflictException("Only a pending approval request can be cancelled.");

        if (cancelledByUserId != RequesterUserId)
            throw new ForbiddenException("Only the requester may cancel this approval request.");

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
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>ApprovalDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private void SkipPendingSteps()
    {
        foreach (var step in _steps.Where(x => x.IsPending))
            step.Skip();
    }
}
