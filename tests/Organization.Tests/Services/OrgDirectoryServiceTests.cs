using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Api.Services;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Services;

public class OrgDirectoryServiceTests
{
    private static async Task<(Company company, OrgUnit unit)> SeedCompanyAndUnitAsync(
        OrganizationTestHost host, OrgUnit? parent = null)
    {
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var unit = new OrgUnit
        {
            CompanyId = company.Id,
            ParentId = parent?.Id,
            Type = OrgUnitType.Department,
            Name = "Engineering",
            Code = "ENG",
        };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (company, unit);
    }

    private static Employee MakeEmployee(
        string companyId, string code, string? userId = "user-" + nameof(Employee),
        EmploymentStatus status = EmploymentStatus.Active) => new()
    {
        CompanyId = companyId,
        EmployeeCode = code,
        FirstName = code,
        LastName = "Last",
        UserId = userId,
        EmploymentStatus = status,
    };

    [Fact]
    public async Task GetApproverCandidatesAsync_ShouldPreferManagers_WhenBothExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var (company, unit) = await SeedCompanyAndUnitAsync(host);
        var requester = MakeEmployee(company.Id, "REQ", userId: "user-req");
        var manager = MakeEmployee(company.Id, "MGR", userId: "user-mgr");
        var nonManager = MakeEmployee(company.Id, "STAFF", userId: "user-staff");
        await host.Context.Employees.AddRangeAsync(requester, manager, nonManager);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddRangeAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = requester.Id, OrgUnitId = unit.Id, IsPrimary = true, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = manager.Id, OrgUnitId = unit.Id, IsManager = true, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = nonManager.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var candidates = await service.GetApproverCandidatesAsync(
            requester.Id, TestContext.Current.CancellationToken);

        // Assert
        var candidate = Assert.Single(candidates);
        Assert.Equal(manager.Id, candidate.EmployeeId);
    }

    [Fact]
    public async Task GetApproverCandidatesAsync_ShouldFallBackToAllActiveMembers_WhenNoneAreManagers()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var (company, unit) = await SeedCompanyAndUnitAsync(host);
        var requester = MakeEmployee(company.Id, "REQ", userId: "user-req");
        var peerA = MakeEmployee(company.Id, "PEER-A", userId: "user-peer-a");
        var peerB = MakeEmployee(company.Id, "PEER-B", userId: "user-peer-b");
        await host.Context.Employees.AddRangeAsync(requester, peerA, peerB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddRangeAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = requester.Id, OrgUnitId = unit.Id, IsPrimary = true, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = peerA.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = peerB.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var candidates = await service.GetApproverCandidatesAsync(
            requester.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, x => x.EmployeeId == peerA.Id);
        Assert.Contains(candidates, x => x.EmployeeId == peerB.Id);
    }

    [Fact]
    public async Task GetApproverCandidatesAsync_ShouldExcludeSelfInactiveAndUnlinked()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var (company, unit) = await SeedCompanyAndUnitAsync(host);
        var requester = MakeEmployee(company.Id, "REQ", userId: "user-req");
        var inactive = MakeEmployee(company.Id, "INACTIVE", userId: "user-inactive", status: EmploymentStatus.Terminated);
        var unlinked = MakeEmployee(company.Id, "UNLINKED", userId: null);
        await host.Context.Employees.AddRangeAsync(requester, inactive, unlinked);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddRangeAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = requester.Id, OrgUnitId = unit.Id, IsPrimary = true, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = inactive.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = unlinked.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow,
            });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var candidates = await service.GetApproverCandidatesAsync(
            requester.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetApproverCandidatesAsync_ShouldClimbToParent_WhenCurrentUnitHasNoCandidates()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var parentUnit = new OrgUnit
        {
            CompanyId = company.Id, Type = OrgUnitType.Department, Name = "Division", Code = "DIV",
        };
        await host.Context.OrgUnits.AddAsync(parentUnit, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var childUnit = new OrgUnit
        {
            CompanyId = company.Id, ParentId = parentUnit.Id, Type = OrgUnitType.Team, Name = "Team", Code = "TEAM",
        };
        await host.Context.OrgUnits.AddAsync(childUnit, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requester = MakeEmployee(company.Id, "REQ", userId: "user-req");
        var divisionManager = MakeEmployee(company.Id, "DIV-MGR", userId: "user-div-mgr");
        await host.Context.Employees.AddRangeAsync(requester, divisionManager);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddRangeAsync(
            new EmployeeOrgUnitMembership
            {
                EmployeeId = requester.Id, OrgUnitId = childUnit.Id, IsPrimary = true, StartDate = DateTimeOffset.UtcNow,
            },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = divisionManager.Id, OrgUnitId = parentUnit.Id, IsManager = true, StartDate = DateTimeOffset.UtcNow,
            });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var candidates = await service.GetApproverCandidatesAsync(
            requester.Id, TestContext.Current.CancellationToken);

        // Assert
        var candidate = Assert.Single(candidates);
        Assert.Equal(divisionManager.Id, candidate.EmployeeId);
    }

    [Fact]
    public async Task GetApproverCandidatesAsync_ShouldReturnEmpty_WhenNoPrimaryMembership()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requester = MakeEmployee(company.Id, "REQ", userId: "user-req");
        await host.Context.Employees.AddAsync(requester, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var candidates = await service.GetApproverCandidatesAsync(
            requester.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetEmployeeNameAsync_ShouldReturnFullName_WhenEmployeeExists()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee
        {
            CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe",
        };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new OrgDirectoryService(host.Context);

        // Act
        var name = await service.GetEmployeeNameAsync(employee.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Jane Doe", name);
    }

    [Fact]
    public async Task GetEmployeeNameAsync_ShouldReturnNull_WhenEmployeeDoesNotExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var service = new OrgDirectoryService(host.Context);

        // Act
        var name = await service.GetEmployeeNameAsync("missing", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(name);
    }
}
