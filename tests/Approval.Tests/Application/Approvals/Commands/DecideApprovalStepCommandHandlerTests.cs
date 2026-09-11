using Light.Contracts;
using Moq;
using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Contracts.Services;
using Xunit;

namespace Approval.Tests.Application.Approvals.Commands;

public class DecideApprovalStepCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToApprovalService_WithTheSameArguments()
    {
        // Arrange
        var serviceMock = new Mock<IApprovalService>();
        serviceMock
            .Setup(s => s.DecideAsync("req-1", "user-1", true, "Looks good", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = new DecideApprovalStepCommandHandler(serviceMock.Object);

        // Act
        var result = await handler.Handle(
            new DecideApprovalStepCommand("req-1", "user-1", true, "Looks good"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        serviceMock.Verify(
            s => s.DecideAsync("req-1", "user-1", true, "Looks good", It.IsAny<CancellationToken>()), Times.Once);
    }
}
