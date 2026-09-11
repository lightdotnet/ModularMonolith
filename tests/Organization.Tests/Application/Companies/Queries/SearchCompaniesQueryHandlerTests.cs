using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Companies.Queries;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Common;
using StarterKit.Organization.Contracts.Companies;
using Xunit;

namespace Organization.Tests.Application.Companies.Queries;

public class SearchCompaniesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFilterBySearchValue_AcrossNameAndCode()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        await host.Context.Companies.AddRangeAsync(
            new Company { Name = "Acme Corp", Code = "ACME" },
            new Company { Name = "Globex", Code = "GLBX" });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchCompaniesQueryHandler(host.Context);

        // Act: SQLite translates .Contains() to instr(), which is byte-for-byte case-sensitive
        // (unlike SQL Server's default collation) — match the stored casing, same as every other
        // handler in this codebase using this exact WhereIf(...Contains...) pattern.
        var result = await handler.Handle(
            new SearchCompaniesQuery(new CompanySearchRequest { SearchValue = "Acme", PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Acme Corp", result.Data.Records.Single().Name);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        await host.Context.Companies.AddRangeAsync(
            new Company { Name = "Active Co", Code = "AC", Status = OrganizationStatus.Active },
            new Company { Name = "Inactive Co", Code = "IC", Status = OrganizationStatus.Inactive });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchCompaniesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchCompaniesQuery(new CompanySearchRequest { Status = OrganizationStatus.Inactive, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Inactive Co", result.Data.Records.Single().Name);
    }
}
