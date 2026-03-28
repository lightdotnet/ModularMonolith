namespace Monolith.Identity.Extensions;

public static class IdentityResultExtensions
{
    public static Result ToResult(this Microsoft.AspNetCore.Identity.IdentityResult result)
    {
        var error = string.Join("|", result.Errors.Select(e => e.Description));

        return result.Succeeded
            ? Result.Success()
            : Result.Error(error);
    }

    public static Result<T> ToResult<T>(this Microsoft.AspNetCore.Identity.IdentityResult result, T data)
    {
        var error = string.Join("|", result.Errors.Select(e => e.Description));

        return result.Succeeded
            ? Result<T>.Success(data: data)
            : Result<T>.Error(error);
    }
}