using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Identity.Web.Pages.Admin;
using StarterKit.Modules.Identity.Web.TagHelpers;
using Xunit;
using IResult = Light.Contracts.IResult;
using ValidationException = Light.Exceptions.ValidationException;

namespace Identity.Tests.Web.Pages.Admin;

public class AdminPageModelTests
{
    public sealed record TestCommand : IRequest<IResult>;

    public sealed class TestInput
    {
        public string? Name { get; set; }

        public List<string> Roles { get; set; } = [];
    }

    private sealed class TestPage : AdminPageModel
    {
        public TestInput Input { get; set; } = new();

        public int PageSize { get; set; }

        public Task<IResult?> CallSendAsync(
            IRequest<IResult> request,
            string inputPrefix = DefaultInputPrefix) =>
            SendAsync(request, inputPrefix);

        public IActionResult? CallHandleResult(
            IResult result,
            string successMessage,
            Func<IActionResult>? redirect = null,
            string errorKey = "") =>
            HandleResult(result, successMessage, redirect, errorKey);

        public IActionResult CallRedirectWithResult(IResult? result, string successMessage) =>
            RedirectWithResult(result, successMessage);
    }

    private static TestPage CreateSut(Action<Mock<IMediator>>? setupMediator = null)
    {
        var mediatorMock = new Mock<IMediator>();
        setupMediator?.Invoke(mediatorMock);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        return new TestPage
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public void HandleResult_OnSuccess_ShouldSetSuccessFlashAndRedirectToCurrentPage()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = sut.CallHandleResult(Result.Success(), "Saved.");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(response);
        Assert.Null(redirect.PageName);
        Assert.Equal("Saved.", sut.TempData[FlashMessages.SuccessKey]);
        Assert.True(sut.ModelState.IsValid);
    }

    [Fact]
    public void HandleResult_OnSuccess_ShouldUseTheGivenRedirect()
    {
        // Arrange
        var sut = CreateSut();
        var expected = new RedirectToPageResult("/Admin/Users/Index");

        // Act
        var response = sut.CallHandleResult(Result.Success(), "Saved.", () => expected);

        // Assert
        Assert.Same(expected, response);
    }

    [Fact]
    public void HandleResult_OnFailure_ShouldAddEachErrorToModelStateAndReturnNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = sut.CallHandleResult(Result.Error("Name is taken.|Name is too short."), "Saved.");

        // Assert
        Assert.Null(response);
        Assert.False(sut.TempData.ContainsKey(FlashMessages.SuccessKey));
        var errors = sut.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage);
        Assert.Equal(["Name is taken.", "Name is too short."], errors);
    }

    [Fact]
    public void HandleResult_OnFailure_ShouldUseErrorKeyAndDefaultMessage()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = sut.CallHandleResult(Result.Error(), "Saved.", errorKey: "Password.NewPassword");

        // Assert
        Assert.Null(response);
        var error = Assert.Single(sut.ModelState["Password.NewPassword"]!.Errors);
        Assert.Equal(AdminPageModel.DefaultErrorMessage, error.ErrorMessage);
    }

    [Fact]
    public void RedirectWithResult_OnFailure_ShouldSetErrorFlashAndRedirect()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = sut.CallRedirectWithResult(Result.NotFound("User 1 not found"), "Deleted.");

        // Assert
        Assert.IsType<RedirectToPageResult>(response);
        Assert.Equal("User 1 not found", sut.TempData[FlashMessages.ErrorKey]);
        Assert.False(sut.TempData.ContainsKey(FlashMessages.SuccessKey));
        Assert.True(sut.ModelState.IsValid);
    }

    [Fact]
    public void RedirectWithResult_OnSuccess_ShouldSetSuccessFlashAndRedirect()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = sut.CallRedirectWithResult(Result.Success(), "Deleted.");

        // Assert
        Assert.IsType<RedirectToPageResult>(response);
        Assert.Equal("Deleted.", sut.TempData[FlashMessages.SuccessKey]);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnResponse_WhenTheRequestIsValid()
    {
        // Arrange
        var expected = Result.Success();
        var sut = CreateSut(mediator => mediator
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected));

        // Act
        var response = await sut.CallSendAsync(new TestCommand());

        // Assert
        Assert.Same(expected, response);
        Assert.True(sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SendAsync_ShouldMapValidationErrorsOntoTheInputPrefix()
    {
        // Arrange
        var sut = CreateSut(mediator => mediator
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(new Dictionary<string, string[]>
            {
                ["Model.Name"] = ["Name is required.", "Name is too short."],
                ["Model.Roles[0]"] = ["Unknown role."],
            })));

        // Act
        var response = await sut.CallSendAsync(new TestCommand());

        // Assert
        Assert.Null(response);
        Assert.Equal(
            ["Name is required.", "Name is too short."],
            sut.ModelState["Input.Name"]!.Errors.Select(e => e.ErrorMessage));
        Assert.Equal("Unknown role.", Assert.Single(sut.ModelState["Input.Roles[0]"]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_ShouldPutUnmappableErrorsUnderTheModelLevelKey()
    {
        // Arrange
        var sut = CreateSut(mediator => mediator
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(new Dictionary<string, string[]>
            {
                ["Id"] = ["Id is required."],
                ["Model.Unknown"] = ["Something is wrong."],
            })));

        // Act
        var response = await sut.CallSendAsync(new TestCommand());

        // Assert
        Assert.Null(response);
        Assert.Equal(
            ["Id is required.", "Something is wrong."],
            sut.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task SendAsync_ShouldMapOntoThePageProperties_WhenThePrefixIsEmpty()
    {
        // Arrange
        var sut = CreateSut(mediator => mediator
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(new Dictionary<string, string[]>
            {
                ["Model.PageSize"] = ["Page size is out of range."],
            })));

        // Act
        var response = await sut.CallSendAsync(new TestCommand(), inputPrefix: string.Empty);

        // Assert
        Assert.Null(response);
        Assert.Single(sut.ModelState["PageSize"]!.Errors);
    }

    [Fact]
    public void RedirectWithResult_WhenResultIsNull_ShouldFlashTheModelStateErrors()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError(string.Empty, "Id is required.");

        // Act
        var response = sut.CallRedirectWithResult(null, "Deleted.");

        // Assert
        Assert.IsType<RedirectToPageResult>(response);
        Assert.Equal("Id is required.", sut.TempData[FlashMessages.ErrorKey]);
    }
}
