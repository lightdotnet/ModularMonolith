using System.Reflection;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;

namespace Approval.Tests.TestSupport;

/// <summary>
/// Builds <see cref="ApprovalRequest"/> / <see cref="ApprovalStep"/> aggregates directly in an
/// arbitrary state for read-model seeding.
/// <para>
/// The domain types encapsulate all state behind private setters and a factory
/// (<see cref="ApprovalRequest.Create"/>) that only ever yields a fresh <c>Pending</c> request.
/// Query / read-handler tests need pre-existing rows in specific terminal or mid-chain states,
/// so this test-only helper writes the private members directly — exactly what the pre-DDD
/// object initializers in these tests used to do.
/// </para>
/// </summary>
internal static class ApprovalEntityBuilder
{
    public static ApprovalStep Step(
        int level,
        string approverUserId,
        string approverEmployeeId,
        ApprovalStepStatus status = ApprovalStepStatus.Pending,
        string? approverName = null,
        DateTimeOffset? decidedAt = null,
        string? comment = null)
    {
        var step = New<ApprovalStep>();

        Set(step, nameof(ApprovalStep.Level), level);
        Set(step, nameof(ApprovalStep.ApproverUserId), approverUserId);
        Set(step, nameof(ApprovalStep.ApproverEmployeeId), approverEmployeeId);
        Set(step, nameof(ApprovalStep.ApproverName), approverName);
        Set(step, nameof(ApprovalStep.Status), status);
        Set(step, nameof(ApprovalStep.DecidedAt), decidedAt);
        Set(step, nameof(ApprovalStep.Comment), comment);

        return step;
    }

    public static ApprovalRequest Request(
        string requestType,
        string requestId,
        string requesterUserId,
        string title,
        ApprovalStatus status = ApprovalStatus.Pending,
        int currentLevel = 1,
        IEnumerable<ApprovalStep>? steps = null,
        string? requesterEmployeeId = null,
        string? requesterName = null,
        string? documentTypeId = null,
        DateTimeOffset? finalizedAt = null)
    {
        var request = New<ApprovalRequest>();

        Set(request, nameof(ApprovalRequest.RequestType), requestType);
        Set(request, nameof(ApprovalRequest.RequestId), requestId);
        Set(request, nameof(ApprovalRequest.RequesterUserId), requesterUserId);
        Set(request, nameof(ApprovalRequest.RequesterEmployeeId), requesterEmployeeId);
        Set(request, nameof(ApprovalRequest.RequesterName), requesterName);
        Set(request, nameof(ApprovalRequest.Title), title);
        Set(request, nameof(ApprovalRequest.Status), status);
        Set(request, nameof(ApprovalRequest.CurrentLevel), currentLevel);
        Set(request, nameof(ApprovalRequest.DocumentTypeId), documentTypeId);
        Set(request, nameof(ApprovalRequest.FinalizedAt), finalizedAt);

        if (steps is not null)
            StepsBackingList(request).AddRange(steps);

        return request;
    }

    public static void SetTitle(ApprovalRequest request, string title) =>
        Set(request, nameof(ApprovalRequest.Title), title);

    private static T New<T>() =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static List<ApprovalStep> StepsBackingList(ApprovalRequest request)
    {
        var field = typeof(ApprovalRequest).GetField("_steps", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ApprovalRequest._steps backing field not found.");

        return (List<ApprovalStep>)field.GetValue(request)!;
    }

    private static void Set(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }
}
