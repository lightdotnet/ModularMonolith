using Organization.Tests.TestSupport;
using StarterKit.Organization.Api.Application.Companies.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Companies;
using Xunit;

namespace Organization.Tests.Application.Companies.Commands;

public class CreateCompanyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenCodeAlreadyExists()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        await host.Context.Companies.AddAsync(new Company { Name = "Acme", Code = "ACME" }, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCompanyCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateCompanyCommand(new CreateCompanyRequest { Name = "Acme Corp", Code = "ACME" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenCodeIsUnique()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var handler = new CreateCompanyCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateCompanyCommand(new CreateCompanyRequest { Name = "Acme", Code = "ACME" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(await host.Context.Companies.FindAsync([result.Data], TestContext.Current.CancellationToken));
    }
}
