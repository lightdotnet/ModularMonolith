using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.Employees.Commands;

public class RemoveEmployeeFromOrgUnitCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSetEndDate_NotHardDeleteTheMembership()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U", Code = "U" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id,
                OrgUnitId = unit.Id,
                IsPrimary = true,
                StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveEmployeeFromOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new RemoveEmployeeFromOrgUnitCommand(employee.Id, unit.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var membership = host.Context.EmployeeOrgUnitMemberships
            .Single(x => x.EmployeeId == employee.Id && x.OrgUnitId == unit.Id);
        Assert.NotNull(membership.EndDate);
        Assert.False(membership.IsPrimary);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNoActiveMembershipExists()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var handler = new RemoveEmployeeFromOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new RemoveEmployeeFromOrgUnitCommand("missing-employee", "missing-unit"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
