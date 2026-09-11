using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.OrgUnits.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.OrgUnits.Commands;

public class DeleteOrgUnitCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenUnitHasChildren()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var parent = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "P", Code = "P" };
        await host.Context.OrgUnits.AddAsync(parent, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddAsync(
            new OrgUnit { CompanyId = company.Id, ParentId = parent.Id, Type = OrgUnitType.Team, Name = "C", Code = "C" },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteOrgUnitCommand(parent.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenUnitHasActiveMembers()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "P", Code = "P" };
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id,
                OrgUnitId = unit.Id,
                StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteOrgUnitCommand(unit.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenOnlyEndedMembershipsExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "P", Code = "P" };
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id,
                OrgUnitId = unit.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteOrgUnitCommand(unit.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
