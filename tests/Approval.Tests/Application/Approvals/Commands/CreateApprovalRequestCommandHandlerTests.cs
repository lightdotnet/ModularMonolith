using Light.Contracts;
using Moq;
using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using Xunit;

namespace Approval.Tests.Application.Approvals.Commands;

public class CreateApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToApprovalService_WithTheSameModel()
    {
        // Arrange
        var serviceMock = new Mock<IApprovalService>();
        var model = new CreateApprovalRequest(
            "Leave", "req-1", "requester-1", "emp-1", null, "Title", null, null, null,
            [new ApproverStepInput(1, "approver-1", "approver-1")]);
        serviceMock
            .Setup(s => s.CreateAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("new-id"));
        var handler = new CreateApprovalRequestCommandHandler(serviceMock.Object);

        // Act
        var result = await handler.Handle(new CreateApprovalRequestCommand(model), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-id", result.Data);
        serviceMock.Verify(s => s.CreateAsync(model, It.IsAny<CancellationToken>()), Times.Once);
    }
}
