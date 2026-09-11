using Moq;
using StarterKit.Approval.Api.Application.Approvals.EventHandlers;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;
using Xunit;

namespace Approval.Tests.Application.Approvals.EventHandlers;

public class ApprovalRequestCancelledEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNotifyTheCurrentApprover_WithApprovalsOwnDecisionPageUrl()
    {
        // Arrange — DeepLinkUrl intentionally differs from the expected Url: same reasoning as
        // ApprovalStepPendingEventHandler, the recipient is the approver, not the requester.
        var notification = new ApprovalRequestCancelledEvent(
            "req-1",
            "Leave request",
            "/leave-requests/req-1",
            "requester-1",
            "requester-1",
            "approver-1");
        var serviceMock = new Mock<INotificationService>();
        var handler = new ApprovalRequestCancelledEventHandler(serviceMock.Object);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        serviceMock.Verify(
            s => s.SendAsync(
                notification.CancelledByUserId,
                null,
                notification.CurrentApproverUserId!,
                It.Is<SystemMessage>(m =>
                    m.Url == $"/approvals/requests/{notification.ApprovalRequestId}" &&
                    !string.IsNullOrEmpty(m.Title) &&
                    m.Message != null && m.Message.Contains(notification.Title)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotNotify_WhenThereIsNoCurrentApprover()
    {
        // Arrange — the request was withdrawn before any level had a pending approver.
        var notification = new ApprovalRequestCancelledEvent(
            "req-1",
            "Leave request",
            "/leave-requests/req-1",
            "requester-1",
            "requester-1",
            null);
        var serviceMock = new Mock<INotificationService>();
        var handler = new ApprovalRequestCancelledEventHandler(serviceMock.Object);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        serviceMock.Verify(
            s => s.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<SystemMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
