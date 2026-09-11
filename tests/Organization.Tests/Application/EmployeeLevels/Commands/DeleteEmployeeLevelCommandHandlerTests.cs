using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.EmployeeLevels.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.EmployeeLevels.Commands;

/// <summary>
/// Deleting an employee level is intentionally unguarded: the FK is configured with
/// <c>DeleteBehavior.SetNull</c> (see <c>OrganizationDbContext.ConfigureModel</c>), so any
/// membership referencing the deleted level just loses its level rather than blocking the delete.
/// This test documents that behavior rather than flags it as a bug.
/// </summary>
public class DeleteEmployeeLevelCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNullOutReferencingMemberships_WhenLevelIsInUse()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var level = new EmployeeLevel { CompanyId = company.Id, Name = "Senior", Code = "SR", Rank = 3 };
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U", Code = "U" };
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.EmployeeLevels.AddAsync(level, TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id, OrgUnitId = unit.Id, LevelId = level.Id, StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeLevelCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteEmployeeLevelCommand(level.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var membership = host.Context.EmployeeOrgUnitMemberships.Single(x => x.EmployeeId == employee.Id);
        Assert.Null(membership.LevelId);
    }
}
