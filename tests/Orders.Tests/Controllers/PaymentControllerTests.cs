using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Api.Application.Payments.Queries;
using StarterKit.Orders.Api.Controllers;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Payments;
using StarterKit.Shared;
using StarterKit.Shared.Constants;
using Xunit;

namespace Orders.Tests.Controllers;

/// <summary>Mirrors <see cref="OrderControllerTests"/>'s thin mediator-dispatch depth.</summary>
public class PaymentControllerTests
{
    private const string CurrentUserId = "current-user";

    private static (PaymentController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new PaymentController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task GetByOrderAsync_ShouldDispatchQuery()
    {
        // GetPaymentsByOrderQuery returns a raw IReadOnlyList<PaymentDto> (not an IResult), so
        // ApiControllerBase.Ok<T>() auto-wraps it into the Result<T> envelope — do not hand-wrap.
        var (controller, mediatorMock) = CreateSut();
        IReadOnlyList<PaymentDto> expected = [new PaymentDto { Id = 1 }];
        mediatorMock
            .Setup(m => m.Send(It.Is<GetPaymentsByOrderQuery>(q => q.OrderId == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.GetByOrderAsync(1);

        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<IReadOnlyList<PaymentDto>>>(objectResult.Value);
        Assert.Same(expected, wrapped.Data);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCommand_AsTheCurrentUser()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new RecordPaymentRequest
        {
            Amount = 50m,
            Currency = CurrencyConstants.Default,
            Method = PaymentMethod.Cash,
            PaidAt = DateTimeOffset.UtcNow,
        };
        var expected = Result<long>.Success(1);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<RecordPaymentCommand>(c =>
                    c.OrderId == 1 && c.Model == request && c.RecordedByUserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.PostAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task VoidAsync_ShouldDispatchCommand()
    {
        var (controller, mediatorMock) = CreateSut();
        var request = new VoidPaymentRequest { Reason = "refund" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<VoidPaymentCommand>(c => c.PaymentId == 1 && c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var response = await controller.VoidAsync(1, request);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
