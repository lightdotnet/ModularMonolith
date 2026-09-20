using FluentValidation.TestHelper;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Validators.Stock;

/// <summary>
/// Covers both validator layers for <c>RecordStockMovement</c>: the Contracts-level
/// <see cref="RecordStockMovementRequestValidator"/> standalone, and the Api-level
/// <see cref="RecordStockMovementCommandValidator"/> that delegates to it via
/// <c>SetValidator</c>. Mirrors <c>Location.Tests.Validators.Locations.CreateLocationValidatorTests</c>.
/// </summary>
public class RecordStockMovementRequestValidatorTests
{
    private static RecordStockMovementRequest ValidRequest() =>
        new()
        {
            ProductId = 1,
            LocationId = "location-1",
            QuantityDelta = 5,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new RecordStockMovementRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenProductIdIsNotPositive()
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { ProductId = 0 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenLocationIdIsEmpty(string? locationId)
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { LocationId = locationId! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LocationId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenLocationIdExceedsMaxLength()
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { LocationId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LocationId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenQuantityDeltaIsZero()
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { QuantityDelta = 0 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.QuantityDelta);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    public void RequestValidator_ShouldNotHaveError_WhenQuantityDeltaIsNonZero(int quantityDelta)
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { QuantityDelta = quantityDelta };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.QuantityDelta);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNoteExceedsMaxLength()
    {
        var validator = new RecordStockMovementRequestValidator();
        var request = ValidRequest() with { Note = new string('a', 501) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenModelIsValid()
    {
        var validator = new RecordStockMovementCommandValidator();
        var command = new RecordStockMovementCommand(ValidRequest(), "user-1");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CommandValidator_ShouldHaveError_WhenPerformedByUserIdIsEmpty(string? performedByUserId)
    {
        var validator = new RecordStockMovementCommandValidator();
        var command = new RecordStockMovementCommand(ValidRequest(), performedByUserId!);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PerformedByUserId);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelQuantityDeltaIsZero()
    {
        var validator = new RecordStockMovementCommandValidator();
        var command = new RecordStockMovementCommand(ValidRequest() with { QuantityDelta = 0 }, "user-1");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.QuantityDelta);
    }
}
