using Identity.Tests.TestSupport;
using StarterKit.Identity.Api.Application.Users.Queries;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Application.Users.Queries;

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

    [Theory]
    [InlineData("userName", null, new[] { "alpha", "bravo", "charlie" })]
    [InlineData("userName", "asc", new[] { "alpha", "bravo", "charlie" })]
    [InlineData("USERNAME", "DESC", new[] { "charlie", "bravo", "alpha" })]
    [InlineData("email", "desc", new[] { "alpha", "charlie", "bravo" })]
    [InlineData("created", "asc", new[] { "bravo", "charlie", "alpha" })]
    [InlineData("created", "desc", new[] { "alpha", "charlie", "bravo" })]
    [InlineData("firstName", "asc", new[] { "charlie", "alpha", "bravo" })]
    [InlineData("lastName", "desc", new[] { "bravo", "charlie", "alpha" })]
    [InlineData("status", "asc", new[] { "charlie", "alpha", "bravo" })]
    public async Task Handle_ShouldApplyRequestedSort(
        string sortBy,
        string? sortDirection,
        string[] expectedUserNames)
    {
        // Arrange
        using var host = await CreateSortSeedAsync();
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var result = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = sortBy,
                SortDirection = sortDirection,
            }),
            CancellationToken.None);

        // Assert
        Assert.Equal(
            expectedUserNames,
            result.Data.Records.Select(x => x.UserName));
    }

    [Fact]
    public async Task Handle_FullNameSort_ShouldOrderByFirstThenLastName()
    {
        // Arrange
        using var host = new IdentityTestHost();
        await CreateUserAsync(host, "u1", firstName: "Ann", lastName: "Zed", email: null, created: 1);
        await CreateUserAsync(host, "u2", firstName: "Ann", lastName: "Bee", email: null, created: 2);
        await CreateUserAsync(host, "u3", firstName: "Bob", lastName: "Aye", email: null, created: 3);
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var ascending = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = SearchUserSortFields.FullName,
            }),
            CancellationToken.None);

        var descending = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = SearchUserSortFields.FullName,
                SortDirection = "desc",
            }),
            CancellationToken.None);

        // Assert
        Assert.Equal(
            ["u2", "u1", "u3"],
            ascending.Data.Records.Select(x => x.UserName));

        Assert.Equal(
            ["u3", "u1", "u2"],
            descending.Data.Records.Select(x => x.UserName));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "asc")]
    [InlineData("passwordHash", "asc")]
    [InlineData("", "desc")]
    public async Task Handle_WithAbsentOrUnknownSort_ShouldUseDefaultOrdering(
        string? sortBy,
        string? sortDirection)
    {
        // Arrange
        using var host = await CreateSortSeedAsync();
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var result = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = sortBy,
                SortDirection = sortDirection,
            }),
            CancellationToken.None);

        // Assert: default ordering is newest first.
        Assert.Equal(
            ["alpha", "charlie", "bravo"],
            result.Data.Records.Select(x => x.UserName));
    }

    [Fact]
    public async Task Handle_DefaultOrdering_ShouldBreakCreatedTiesByUserName()
    {
        // Arrange
        using var host = new IdentityTestHost();
        await CreateUserAsync(host, "zulu", firstName: null, lastName: null, email: null, created: 1);
        await CreateUserAsync(host, "yankee", firstName: null, lastName: null, email: null, created: 1);
        await CreateUserAsync(host, "xray", firstName: null, lastName: null, email: null, created: 2);
        var handler = new SearchUserQueryHandler(host.UserManager);

        // Act
        var result = await handler.Handle(
            new SearchUserQuery(new SearchUserRequest
            {
                PageNumber = 1,
                PageSize = 10,
            }),
            CancellationToken.None);

        // Assert
        Assert.Equal(
            ["xray", "yankee", "zulu"],
            result.Data.Records.Select(x => x.UserName));
    }

    /// <summary>
    /// Three users whose fields order differently per column:
    /// created bravo &lt; charlie &lt; alpha; email alpha(z) &gt; charlie(m) &gt; bravo(a);
    /// first name charlie(A) &lt; alpha(M) &lt; bravo(Z); last name bravo(Z) &gt; charlie(M) &gt; alpha(A);
    /// status charlie(Inactive) &lt; alpha(Active) &lt; bravo(Locked).
    /// </summary>
    private static async Task<IdentityTestHost> CreateSortSeedAsync()
    {
        var host = new IdentityTestHost();

        await CreateUserAsync(host, "bravo", firstName: "Zoe", lastName: "Zulu", email: "a@example.com", created: 1, ActiveStatus.State.Locked);
        await CreateUserAsync(host, "charlie", firstName: "Adam", lastName: "Mike", email: "m@example.com", created: 2, ActiveStatus.State.Inactive);
        await CreateUserAsync(host, "alpha", firstName: "Mary", lastName: "Able", email: "z@example.com", created: 3, ActiveStatus.State.Active);

        return host;
    }

    private static async Task CreateUserAsync(
        IdentityTestHost host,
        string userName,
        string? firstName,
        string? lastName,
        string? email,
        int created,
        ActiveStatus.State status = ActiveStatus.State.Active)
    {
        // Created is stamped from the clock by the DbContext's audit hook.
        host.DateTime.UtcNow = new DateTimeOffset(
            2026,
            1,
            created,
            0,
            0,
            0,
            TimeSpan.Zero);

        var user = new User
        {
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Status = new ActiveStatus(status),
        };

        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
    }
}
