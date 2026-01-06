namespace Monolith;

public class EfCoreResult
{
    public static Result From(int rowsAffected)
    {
        if (rowsAffected > 0)
        {
            return Result.Success();
        }
        else
        {
            return Result.Error("No rows were affected. Please check the operation.");
        }
    }
}
