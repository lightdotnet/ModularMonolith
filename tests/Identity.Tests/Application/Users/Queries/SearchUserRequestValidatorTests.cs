using StarterKit.Identity.Api.Application.Users.Queries;
using StarterKit.Identity.Contracts;
using Xunit;

namespace Identity.Tests.Application.Users.Queries;

public class SearchUserRequestValidatorTests
{
    private readonly SearchUserRequestValidator _validator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("userName", null)]
    [InlineData("USERNAME", "ASC")]
    [InlineData("firstName", "asc")]
    [InlineData("lastName", "desc")]
    [InlineData("fullName", "Desc")]
    [InlineData("email", "asc")]
    [InlineData("created", "desc")]
    [InlineData("status", "asc")]
    [InlineData(null, "desc")]
    public void Validate_ShouldAcceptAllowedValues(
        string? sortBy,
        string? sortDirection)
    {
        // Act
        var result = _validator.Validate(new SearchUserRequest
        {
            SortBy = sortBy,
            SortDirection = sortDirection,
        });

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("passwordHash")]
    [InlineData("id")]
    [InlineData("user_name")]
    public void Validate_ShouldRejectUnknownSortBy(string sortBy)
    {
        // Act
        var result = _validator.Validate(new SearchUserRequest
        {
            SortBy = sortBy,
        });

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(SearchUserRequest.SortBy), error.PropertyName);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("ascending")]
    [InlineData("1")]
    public void Validate_ShouldRejectUnknownSortDirection(string sortDirection)
    {
        // Act
        var result = _validator.Validate(new SearchUserRequest
        {
            SortBy = SearchUserSortFields.Email,
            SortDirection = sortDirection,
        });

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(SearchUserRequest.SortDirection), error.PropertyName);
    }

    [Fact]
    public void QueryValidator_ShouldApplyRequestRulesUnderModelPrefix()
    {
        // Arrange
        var validator = new SearchUserQueryValidator();

        // Act
        var result = validator.Validate(new SearchUserQuery(new SearchUserRequest
        {
            SortBy = "passwordHash",
            SortDirection = "sideways",
        }));

        // Assert
        Assert.Equal(
            ["Model.SortBy", "Model.SortDirection"],
            result.Errors.Select(x => x.PropertyName).Order());
    }
}
