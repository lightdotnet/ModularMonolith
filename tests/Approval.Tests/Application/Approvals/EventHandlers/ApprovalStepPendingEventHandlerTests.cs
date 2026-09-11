using Moq;
using StarterKit.Approval.Api.Application.Approvals.EventHandlers;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;
using Xunit;

namespace Approval.Tests.Application.Approvals.EventHandlers;

public class ApprovalStepPendingEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNotifyTheApprover_WithApprovalsOwnDecisionPageUrl()
    {
        // Arrange — DeepLinkUrl intentionally differs from the expected Url: the approver may not
        // own (or even have access to) the requesting module's own record, so the handler must
        // link to Approval's own decision page instead of the caller-supplied deep link.
        var notification = new ApprovalStepPendingEvent(
            "req-1",
            "Leave request",
            "/leave-requests/req-1",
            "approver-1",
            "requester-1");
        var serviceMock = new Mock<INotificationService>();
        var handler = new ApprovalStepPendingEventHandler(serviceMock.Object);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        serviceMock.Verify(
            s => s.SendAsync(
                notification.RequesterUserId,
                null,
                notification.ApproverUserId,
                It.Is<SystemMessage>(m =>
                    m.Url == $"/approvals/requests/{notification.ApprovalRequestId}" &&
                    !string.IsNullOrEmpty(m.Title) &&
                    m.Message != null && m.Message.Contains(notification.Title)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
