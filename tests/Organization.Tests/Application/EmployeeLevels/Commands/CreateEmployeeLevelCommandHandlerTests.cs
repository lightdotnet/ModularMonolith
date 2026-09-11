using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.EmployeeLevels.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.EmployeeLevels;
using Xunit;

namespace Organization.Tests.Application.EmployeeLevels.Commands;

public class CreateEmployeeLevelCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCompanyDoesNotExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var handler = new CreateEmployeeLevelCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLevelCommand(new CreateEmployeeLevelRequest
            {
                CompanyId = "missing", Name = "Senior", Code = "SR", Rank = 3,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCodeAlreadyExistsInSameCompany()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeLevels.AddAsync(
            new EmployeeLevel { CompanyId = company.Id, Name = "Senior", Code = "SR", Rank = 3 },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeLevelCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLevelCommand(new CreateEmployeeLevelRequest
            {
                CompanyId = company.Id, Name = "Senior Duplicate", Code = "SR", Rank = 4,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAllow_SameCodeAcrossDifferentCompanies()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var companyA = new Company { Name = "A", Code = "A" };
        var companyB = new Company { Name = "B", Code = "B" };
        await host.Context.Companies.AddRangeAsync([companyA, companyB], TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.EmployeeLevels.AddAsync(
            new EmployeeLevel { CompanyId = companyA.Id, Name = "Senior", Code = "SR", Rank = 3 },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeLevelCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLevelCommand(new CreateEmployeeLevelRequest
            {
                CompanyId = companyB.Id, Name = "Senior", Code = "SR", Rank = 3,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
