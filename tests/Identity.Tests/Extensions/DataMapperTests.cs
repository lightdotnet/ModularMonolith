using Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Extensions;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Extensions;

public class DataMapperTests
{
    [Fact]
    public void MapToDto_ForUser_ShouldMapFieldsAndDerivedStatus()
    {
        // Arrange
        var user = new User
        {
            UserName = "jane.doe",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            PhoneNumber = "555-0100",
            AuthProvider = AuthProvider.ActiveDirectory,
            Status = new ActiveStatus(ActiveStatus.State.Locked),
        };

        // Act
        var dto = user.MapToDto();

        // Assert
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("jane.doe", dto.UserName);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("jane@example.com", dto.Email);
        Assert.Equal("555-0100", dto.PhoneNumber);
        Assert.Equal("AD", dto.AuthProvider);
        Assert.Equal("Locked", dto.Status);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void MapToDto_ForUser_ShouldSetIsDeleted_WhenDeletedIsSet()
    {
        // Arrange
        var user = new User { UserName = "deleted.user", Deleted = DateTimeOffset.UtcNow };

        // Act
        var dto = user.MapToDto();

        // Assert
        Assert.True(dto.IsDeleted);
    }

    [Fact]
    public void MapToDto_ForRole_ShouldMapFields()
    {
        // Arrange
        var role = new Role { Name = "Admin", Description = "Administrators" };

        // Act
        var dto = role.MapToDto();

        // Assert
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal("Admin", dto.Name);
        Assert.Equal("Administrators", dto.Description);
    }

    [Fact]
    public async Task MapToDto_ForUserQueryable_ShouldTranslateThroughEfCore()
    {
        // Arrange: prove the compiled expression tree actually translates to SQL, not just
        // in-memory LINQ-to-objects.
        using var host = new IdentityTestHost();
        var user = new User { UserName = "sql.jane", Email = "sql.jane@example.com" };
        var createResult = await host.UserManager.CreateAsync(user);
        Assert.True(createResult.Succeeded);

        // Act
        var dto = await host.Context.Users.AsNoTracking().MapToDto()
            .SingleAsync(u => u.UserName == "sql.jane", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("sql.jane@example.com", dto.Email);
    }

    [Fact]
    public async Task MapToDto_ForRoleQueryable_ShouldTranslateThroughEfCore()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var role = new Role { Name = "sql-role", Description = "SQL role" };
        var createResult = await host.RoleManager.CreateAsync(role);
        Assert.True(createResult.Succeeded);

        // Act
        var dto = await host.Context.Roles.AsNoTracking().MapToDto()
            .SingleAsync(r => r.Name == "sql-role", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal("SQL role", dto.Description);
    }
}
