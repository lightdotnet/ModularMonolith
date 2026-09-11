using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Companies.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.Companies.Commands;

public class DeleteCompanyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenCompanyStillHasOrgUnits()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.OrgUnits.AddAsync(
            new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "Eng", Code = "ENG" },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCompanyCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteCompanyCommand(company.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCompanyStillHasEmployees()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.Employees.AddAsync(
            new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" },
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCompanyCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteCompanyCommand(company.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldDelete_WhenCompanyHasNoDependents()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "Acme", Code = "ACME" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCompanyCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteCompanyCommand(company.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.Context.Companies.FindAsync([company.Id], TestContext.Current.CancellationToken));
    }
}
