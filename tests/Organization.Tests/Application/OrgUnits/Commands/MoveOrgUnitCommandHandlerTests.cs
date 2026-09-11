using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.OrgUnits.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.OrgUnits;
using Xunit;

namespace Organization.Tests.Application.OrgUnits.Commands;

public class MoveOrgUnitCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsSelf()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = await SeedCompanyAsync(host);
        var unit = await SeedOrgUnitAsync(host, company.Id, null, "root");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(unit.Id, new MoveOrgUnitRequest { NewParentId = unit.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentBelongsToDifferentCompany()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var companyA = await SeedCompanyAsync(host, "A");
        var companyB = await SeedCompanyAsync(host, "B");
        var unit = await SeedOrgUnitAsync(host, companyA.Id, null, "root-a");
        var otherCompanyUnit = await SeedOrgUnitAsync(host, companyB.Id, null, "root-b");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(unit.Id, new MoveOrgUnitRequest { NewParentId = otherCompanyUnit.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsOwnDescendant()
    {
        // Arrange: root -> child -> grandchild. Moving root under grandchild must be rejected.
        using var host = new OrganizationTestHost();
        var company = await SeedCompanyAsync(host);
        var root = await SeedOrgUnitAsync(host, company.Id, null, "root");
        var child = await SeedOrgUnitAsync(host, company.Id, root.Id, "child");
        var grandchild = await SeedOrgUnitAsync(host, company.Id, child.Id, "grandchild");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(root.Id, new MoveOrgUnitRequest { NewParentId = grandchild.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldClearParent_WhenNewParentIdIsEmpty()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = await SeedCompanyAsync(host);
        var root = await SeedOrgUnitAsync(host, company.Id, null, "root");
        var child = await SeedOrgUnitAsync(host, company.Id, root.Id, "child");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(child.Id, new MoveOrgUnitRequest { NewParentId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.OrgUnits.FindAsync([child.Id], TestContext.Current.CancellationToken);
        Assert.Null(updated!.ParentId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNewParentDoesNotExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = await SeedCompanyAsync(host);
        var unit = await SeedOrgUnitAsync(host, company.Id, null, "root");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(unit.Id, new MoveOrgUnitRequest { NewParentId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldMove_WhenTargetIsValidSiblingSubtree()
    {
        // Arrange: two independent roots in the same company; move root-b under root-a.
        using var host = new OrganizationTestHost();
        var company = await SeedCompanyAsync(host);
        var rootA = await SeedOrgUnitAsync(host, company.Id, null, "root-a");
        var rootB = await SeedOrgUnitAsync(host, company.Id, null, "root-b");
        var handler = new MoveOrgUnitCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveOrgUnitCommand(rootB.Id, new MoveOrgUnitRequest { NewParentId = rootA.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.OrgUnits.FindAsync([rootB.Id], TestContext.Current.CancellationToken);
        Assert.Equal(rootA.Id, updated!.ParentId);
    }

    private static async Task<Company> SeedCompanyAsync(OrganizationTestHost host, string suffix = "")
    {
        var company = new Company { Name = $"Company{suffix}", Code = $"C{suffix}{Guid.NewGuid():N}" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return company;
    }

    private static async Task<OrgUnit> SeedOrgUnitAsync(
        OrganizationTestHost host, string companyId, string? parentId, string name)
    {
        var unit = new OrgUnit
        {
            CompanyId = companyId,
            ParentId = parentId,
            Type = OrgUnitType.Department,
            Name = name,
            Code = $"{name}-{Guid.NewGuid():N}",
        };
        await host.Context.OrgUnits.AddAsync(unit, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return unit;
    }
}
