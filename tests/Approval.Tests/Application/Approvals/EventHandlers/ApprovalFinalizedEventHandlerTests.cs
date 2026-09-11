using Moq;
using StarterKit.Approval.Api.Application.Approvals.EventHandlers;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;
using Xunit;

namespace Approval.Tests.Application.Approvals.EventHandlers;

public class ApprovalFinalizedEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNotifyTheRequester_WithTheCallerSuppliedDeepLinkUrl()
    {
        // Arrange — unlike the pending/cancelled handlers, the recipient here (RequesterUserId)
        // owns the source record, so the caller-supplied DeepLinkUrl is used verbatim.
        var notification = new ApprovalFinalizedEvent(
            "req-1",
            "Leave request",
            "/leave-requests/req-1",
            "requester-1",
            "approver-1",
            ApprovalStatus.Approved);
        var serviceMock = new Mock<INotificationService>();
        var handler = new ApprovalFinalizedEventHandler(serviceMock.Object);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        serviceMock.Verify(
            s => s.SendAsync(
                notification.DecidedByUserId,
                null,
                notification.RequesterUserId,
                It.Is<SystemMessage>(m =>
                    m.Url == notification.DeepLinkUrl &&
                    !string.IsNullOrEmpty(m.Title) &&
                    m.Message != null && m.Message.Contains(notification.Title)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
