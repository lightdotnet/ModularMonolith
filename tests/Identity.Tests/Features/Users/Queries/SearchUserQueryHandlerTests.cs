using Identity.Tests.TestSupport;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Features.Users.Queries;
using StarterKit.Modules.Identity.Models;
using Xunit;

namespace Identity.Tests.Features.Users.Queries;

public class SearchUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldIgnoreShortSearchTerm()
    {
        // Arrange
        using var host = new IdentityTestHost();
        Assert.True((await host.UserManager.CreateAsync(new User { UserName = "alpha" })).Succeeded);
        Assert.True((await host.UserManager.CreateAsync(new User { UserName = "beta" })).Succeeded);
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var result = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest { SearchValue = "a", PageNumber = 1, PageSize = 10 }),
            CancellationToken.None);

        // Assert: a 1-char term is below the >=2 threshold, so the search is ignored and both records come back.
        Assert.Equal(2, result.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_ShouldFilterAcrossUserFields()
    {
        // Arrange
        using var host = new IdentityTestHost();
        Assert.True((await host.UserManager.CreateAsync(new User { UserName = "alpha", Email = "alpha@example.com" })).Succeeded);
        Assert.True((await host.UserManager.CreateAsync(new User { UserName = "beta", Email = "beta@example.com" })).Succeeded);
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var result = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest { SearchValue = "alpha", PageNumber = 1, PageSize = 10 }),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("alpha", result.Data.Records.Single().UserName);
    }
}
