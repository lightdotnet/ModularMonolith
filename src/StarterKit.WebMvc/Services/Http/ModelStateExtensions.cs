using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StarterKit.WebMvc.Services.Http;

public static class ModelStateExtensions
{
    // Backend commands wrap the Contracts DTO in a property named "Model"
    // (e.g. CreateUserCommand(CreateUserRequest Model)), so validator keys arrive as "Model.X".
    private const string CommandModelPrefix = "Model.";

    /// <summary>
    /// Pushes a failed <see cref="ApiResult"/> into <paramref name="modelState"/>: each backend
    /// validation error lands on the matching field (re-rooted under <paramref name="prefix"/>,
    /// e.g. <c>"Input"</c> for a PageModel's bound <c>Input</c> property); a failure without
    /// field errors lands as a model-level error carrying the backend message.
    /// </summary>
    public static void AddApiErrors(
        this ModelStateDictionary modelState,
        ApiResult result,
        string? prefix = null)
    {
        if (result.IsSuccess)
        {
            return;
        }

        if (!result.HasValidationErrors)
        {
            modelState.AddModelError(
                string.Empty,
                result.Message);

            return;
        }

        foreach (var (key, messages) in result.ValidationErrors)
        {
            var fieldKey = key.StartsWith(CommandModelPrefix, StringComparison.OrdinalIgnoreCase)
                ? key[CommandModelPrefix.Length..]
                : key;

            if (!string.IsNullOrEmpty(prefix))
            {
                fieldKey = $"{prefix}.{fieldKey}";
            }

            foreach (var message in messages)
            {
                modelState.AddModelError(
                    fieldKey,
                    message);
            }
        }
    }
}
