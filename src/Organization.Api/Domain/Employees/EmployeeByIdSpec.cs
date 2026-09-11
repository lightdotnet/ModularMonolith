namespace StarterKit.Organization.Api.Domain.Employees;

public class EmployeeByIdSpec : Specification<Employee>
{
    public EmployeeByIdSpec(string employeeId)
    {
        Where(e => e.Id == employeeId);
    }
}
