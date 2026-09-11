using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.Employees.Commands;

/// <summary>
/// Deleting an employee is intentionally unguarded: the FK from
/// <c>EmployeeOrgUnitMembership.EmployeeId</c> is configured with <c>DeleteBehavior.Cascade</c>
/// (see <c>OrganizationDbContext.ConfigureModel</c>), so its memberships are removed along with
/// it rather than blocking the delete. This test documents that behavior.
/// </summary>
public class DeleteEmployeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCascadeDeleteMemberships_WhenEmployeeIsDeleted()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U", Code = "U" };
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(host.Context.EmployeeOrgUnitMemberships.Any(x => x.EmployeeId == employee.Id));
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var handler = new DeleteEmployeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteEmployeeCommand("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
