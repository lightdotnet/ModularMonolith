using Microsoft.AspNetCore.Mvc.ModelBinding;
using StarterKit.WebMvc.Services.Http;
using Xunit;

namespace StarterKit.WebMvc.Tests.Services.Http;

public class ModelStateExtensionsTests
{
    [Fact]
    public void AddApiErrors_ShouldStripCommandModelPrefix()
    {
        // Arrange
        var modelState = new ModelStateDictionary();

        // Act
        modelState.AddApiErrors(Failure(new Dictionary<string, string[]>
        {
            ["Model.Email"] = ["Email is required"],
            ["model.Name"] = ["Too long", "Invalid"],
            ["PageSize"] = ["Too big"],
        }));

        // Assert
        Assert.Equal(["Email is required"], Messages(modelState, "Email"));
        Assert.Equal(["Too long", "Invalid"], Messages(modelState, "Name"));
        Assert.Equal(["Too big"], Messages(modelState, "PageSize"));
        Assert.False(modelState.ContainsKey("Model.Email"));
    }

    [Fact]
    public void AddApiErrors_ShouldRerootUnderPrefix()
    {
        // Arrange
        var modelState = new ModelStateDictionary();

        // Act
        modelState.AddApiErrors(
            Failure(new Dictionary<string, string[]>
            {
                ["Model.Email"] = ["Email is required"],
                ["Name"] = ["Too long"],
            }),
            "Input");

        // Assert
        Assert.Equal(["Email is required"], Messages(modelState, "Input.Email"));
        Assert.Equal(["Too long"], Messages(modelState, "Input.Name"));
    }

    [Fact]
    public void AddApiErrors_WithoutValidationErrors_ShouldAddModelLevelMessage()
    {
        // Arrange
        var modelState = new ModelStateDictionary();

        // Act
        modelState.AddApiErrors(
            new ApiResult
            {
                IsSuccess = false,
                Message = "User is locked",
            },
            "Input");

        // Assert
        Assert.Equal(["User is locked"], Messages(modelState, string.Empty));
    }

    [Fact]
    public void AddApiErrors_OnSuccess_ShouldAddNothing()
    {
        // Arrange
        var modelState = new ModelStateDictionary();

        // Act
        modelState.AddApiErrors(new ApiResult
        {
            IsSuccess = true,
            Message = "ok",
        });

        // Assert
        Assert.True(modelState.IsValid);
        Assert.Equal(0, modelState.ErrorCount);
    }

    private static ApiResult Failure(Dictionary<string, string[]> errors) =>
        new()
        {
            IsSuccess = false,
            Code = ApiResultCodes.BadRequest,
            Message = "Validation failed",
            ValidationErrors = errors,
        };

    private static string[] Messages(
        ModelStateDictionary modelState,
        string key) =>
        modelState.TryGetValue(key, out var entry)
            ? entry.Errors.Select(error => error.ErrorMessage).ToArray()
            : [];
}
