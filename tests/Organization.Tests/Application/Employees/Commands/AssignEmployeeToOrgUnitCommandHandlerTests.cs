using Microsoft.EntityFrameworkCore;
using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.Employees.Commands;

public class AssignEmployeeToOrgUnitCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenOrgUnitBelongsToDifferentCompany()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var companyA = new Company { Name = "A", Code = "A" };
        var companyB = new Company { Name = "B", Code = "B" };
        await host.Context.Companies.AddRangeAsync(companyA, companyB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = companyA.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        var unitInB = new OrgUnit { CompanyId = companyB.Id, Type = OrgUnitType.Department, Name = "U", Code = "U" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddAsync(unitInB, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unitInB.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenAlreadyActivelyAssigned()
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
                StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unit.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAllow_ReassignAfterPriorMembershipEnded()
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
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                EndDate = DateTimeOffset.UtcNow.AddDays(-1),
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unit.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var activeCount = host.Context.EmployeeOrgUnitMemberships
            .Count(x => x.EmployeeId == employee.Id && x.OrgUnitId == unit.Id && x.EndDate == null);
        Assert.Equal(1, activeCount);
    }

    [Fact]
    public async Task Handle_ShouldClearOtherPrimaryMemberships_WhenAssigningNewPrimary()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        var unit1 = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U1", Code = "U1" };
        var unit2 = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U2", Code = "U2" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddRangeAsync(unit1, unit2);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = employee.Id,
                OrgUnitId = unit1.Id,
                IsPrimary = true,
                StartDate = DateTimeOffset.UtcNow,
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(
                employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unit2.Id, IsPrimary = true }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        // AsNoTracking: the primary-clearing step runs via ExecuteUpdateAsync, which bypasses the
        // change tracker, so a tracked read here would return the stale in-memory IsPrimary value.
        var memberships = host.Context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(x => x.EmployeeId == employee.Id && x.EndDate == null)
            .ToList();
        Assert.Single(memberships, x => x.OrgUnitId == unit1.Id && !x.IsPrimary);
        Assert.Single(memberships, x => x.OrgUnitId == unit2.Id && x.IsPrimary);
    }

    [Fact]
    public async Task Handle_ShouldPersistAssignmentTypeAndIsManager()
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
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(
                employee.Id,
                new AssignEmployeeOrgUnitRequest
                {
                    OrgUnitId = unit.Id,
                    AssignmentType = AssignmentType.Acting,
                    IsManager = true,
                }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var membership = host.Context.EmployeeOrgUnitMemberships
            .Single(x => x.EmployeeId == employee.Id && x.OrgUnitId == unit.Id);
        Assert.Equal(AssignmentType.Acting, membership.AssignmentType);
        Assert.True(membership.IsManager);
    }

    [Fact]
    public async Task Handle_ShouldDefaultToCurrentAndNonManager_WhenNotSpecified()
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
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unit.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var membership = host.Context.EmployeeOrgUnitMemberships
            .Single(x => x.EmployeeId == employee.Id && x.OrgUnitId == unit.Id);
        Assert.Equal(AssignmentType.Current, membership.AssignmentType);
        Assert.False(membership.IsManager);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLevelDoesNotExist()
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
        var handler = new AssignEmployeeToOrgUnitCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new AssignEmployeeToOrgUnitCommand(
                employee.Id, new AssignEmployeeOrgUnitRequest { OrgUnitId = unit.Id, LevelId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
