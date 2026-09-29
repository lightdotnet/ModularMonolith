using FluentValidation;
using Light.Mediator;
using StarterKit.Shared;
using Xunit;
using ValidationException = Light.Exceptions.ValidationException;

namespace Framework.Tests.Shared;

public class ValidationBehaviourTests
{
    private sealed record TestRequest(string Name) : IRequest<string>;

    private sealed class AlwaysValidValidator : AbstractValidator<TestRequest>
    { }

    private sealed class FailingValidator : AbstractValidator<TestRequest>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsRegistered()
    {
        // Arrange
        var behaviour = new ValidationBehaviour<TestRequest, string>([]);
        var nextCalled = false;
        Task<string> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        // Act
        var result = await behaviour.Handle(new TestRequest("x"), Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenAllValidatorsPass()
    {
        // Arrange
        var behaviour = new ValidationBehaviour<TestRequest, string>([new AlwaysValidValidator()]);
        var nextCalled = false;
        Task<string> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        // Act
        var result = await behaviour.Handle(new TestRequest("x"), Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_AndNotCallNext_WhenAValidatorFails()
    {
        // Arrange
        var behaviour = new ValidationBehaviour<TestRequest, string>([new FailingValidator()]);
        var nextCalled = false;
        Task<string> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(new TestRequest(""), Next, CancellationToken.None));

        // Assert
        Assert.False(nextCalled);
        Assert.True(exception.ValidationErrors.ContainsKey("Name"));
    }

    [Fact]
    public async Task Handle_ShouldGroupErrorsByProperty_WhenMultipleValidatorsFailSameProperty()
    {
        // Arrange
        var behaviour = new ValidationBehaviour<TestRequest, string>([new FailingValidator(), new FailingValidator()]);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(new TestRequest(""), _ => Task.FromResult("handled"), CancellationToken.None));

        // Assert
        var errors = Assert.Single(exception.ValidationErrors);
        Assert.Equal("Name", errors.Key);
        Assert.Equal(2, errors.Value.Length);
    }
}
