using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.OrgUnits.Queries;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.OrgUnits.Queries;

public class GetOrgUnitTreeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenCompanyHasNoUnits()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var handler = new GetOrgUnitTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetOrgUnitTreeQuery("missing-company"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldBuildMultiLevelTree()
    {
        // Arrange: root -> child -> grandchild, plus a second independent root.
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var root = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "Root", Code = "ROOT" };
        await host.Context.OrgUnits.AddAsync(root, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new OrgUnit { CompanyId = company.Id, ParentId = root.Id, Type = OrgUnitType.Department, Name = "Child", Code = "CHILD" };
        await host.Context.OrgUnits.AddAsync(child, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var grandchild = new OrgUnit { CompanyId = company.Id, ParentId = child.Id, Type = OrgUnitType.Team, Name = "Grandchild", Code = "GC" };
        var secondRoot = new OrgUnit { CompanyId = company.Id, Type = OrgUnitType.Department, Name = "Root2", Code = "ROOT2" };
        await host.Context.OrgUnits.AddRangeAsync(grandchild, secondRoot);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetOrgUnitTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetOrgUnitTreeQuery(company.Id), TestContext.Current.CancellationToken);

        // Assert: two root nodes, first root has one child with one grandchild.
        Assert.Equal(2, result.Count);
        var rootNode = Assert.Single(result, x => x.Id == root.Id);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal(child.Id, childNode.Id);
        var grandchildNode = Assert.Single(childNode.Children);
        Assert.Equal(grandchild.Id, grandchildNode.Id);
        Assert.Empty(grandchildNode.Children);

        var secondRootNode = Assert.Single(result, x => x.Id == secondRoot.Id);
        Assert.Empty(secondRootNode.Children);
    }
}
