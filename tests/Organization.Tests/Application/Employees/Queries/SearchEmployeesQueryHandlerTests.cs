using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Employees.Queries;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.Employees.Queries;

public class SearchEmployeesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFilterByOrgUnit_UsingOnlyActiveMemberships()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unit = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "U", Code = "U" };
        var activeMember = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Active", LastName = "Member" };
        var formerMember = new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "Former", LastName = "Member" };
        var nonMember = new Employee { CompanyId = company.Id, EmployeeCode = "E3", FirstName = "Non", LastName = "Member" };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(activeMember, formerMember, nonMember);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeOrgUnitMemberships.AddRangeAsync(
            new EmployeeOrgUnitMembership { EmployeeId = activeMember.Id, OrgUnitId = unit.Id, StartDate = DateTimeOffset.UtcNow },
            new EmployeeOrgUnitMembership
            {
                EmployeeId = formerMember.Id, OrgUnitId = unit.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10), EndDate = DateTimeOffset.UtcNow,
            });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { OrgUnitId = unit.Id, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(activeMember.Id, result.Data.Records.Single().Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCompanyAndEmploymentStatus()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var companyA = new Company { Name = "A", Code = "A" };
        var companyB = new Company { Name = "B", Code = "B" };
        await host.Context.Companies.AddRangeAsync(companyA, companyB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(
            new Employee { CompanyId = companyA.Id, EmployeeCode = "E1", FirstName = "Active", LastName = "A", EmploymentStatus = EmploymentStatus.Active },
            new Employee { CompanyId = companyA.Id, EmployeeCode = "E2", FirstName = "Terminated", LastName = "A", EmploymentStatus = EmploymentStatus.Terminated },
            new Employee { CompanyId = companyB.Id, EmployeeCode = "E3", FirstName = "Active", LastName = "B", EmploymentStatus = EmploymentStatus.Active });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest
            {
                CompanyId = companyA.Id, EmploymentStatus = EmploymentStatus.Active, PageNumber = 1, PageSize = 10,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Active", result.Data.Records.Single().FirstName);
    }

    [Fact]
    public async Task Handle_ShouldExcludeEmployeesWithoutLinkedUser_WhenLinkedToUserOnlyIsTrue()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(
            new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Linked", LastName = "One", UserId = "user-1" },
            new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "Unlinked", LastName = "Two" });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { LinkedToUserOnly = true, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Linked", result.Data.Records.Single().FirstName);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllEmployees_WhenLinkedToUserOnlyIsNullOrFalse()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(
            new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Linked", LastName = "One", UserId = "user-1" },
            new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "Unlinked", LastName = "Two" });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var nullResult = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { LinkedToUserOnly = null, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);
        var falseResult = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { LinkedToUserOnly = false, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, nullResult.Data.TotalRecords);
        Assert.Equal(2, falseResult.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_ShouldMatchSearchValue_OnEmail()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(
            new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe", Email = "jane.doe@example.com" },
            new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "John", LastName = "Roe", Email = "john.roe@example.com" });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { SearchValue = "jane.doe@example.com", PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Jane", result.Data.Records.Single().FirstName);
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenAnEmployeeHasNoEmail()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddRangeAsync(
            new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe", Email = "jane.doe@example.com" },
            new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "NoEmail", LastName = "Person", Email = null });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { SearchValue = "jane", PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Jane", result.Data.Records.Single().FirstName);
    }

    [Fact]
    public async Task Handle_ShouldOmitSensitivePii_FromSearchResults()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(
            new Employee
            {
                CompanyId = company.Id,
                EmployeeCode = "E1",
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                PhoneNumber = "555-0100",
                NationalId = "SECRET-123",
                DateOfBirth = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Address = "1 Secret Street",
            },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchEmployeesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchEmployeesQuery(new EmployeeSearchRequest { PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        var record = result.Data.Records.Single();
        Assert.Equal("jane.doe@example.com", record.Email);
        Assert.Equal("555-0100", record.PhoneNumber);
        Assert.Null(record.NationalId);
        Assert.Null(record.DateOfBirth);
        Assert.Null(record.Address);
    }
}
