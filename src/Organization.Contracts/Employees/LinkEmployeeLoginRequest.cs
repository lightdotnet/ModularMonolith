namespace StarterKit.Organization.Contracts.Employees;

public record LinkEmployeeLoginRequest
{
    public string UserId { get; set; } = null!;
}
