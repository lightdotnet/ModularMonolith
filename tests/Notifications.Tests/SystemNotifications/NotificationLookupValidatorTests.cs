using StarterKit.Notifications.Contracts.SystemNotifications;
using Xunit;

namespace Notifications.Tests.SystemNotifications;

public class NotificationLookupValidatorTests
{
    private readonly NotificationLookupValidator _validator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("created", null)]
    [InlineData("CREATED", "DESC")]
    [InlineData("title", "asc")]
    [InlineData("status", "desc")]
    [InlineData("fromName", "Asc")]
    public void Validate_ShouldAcceptAllowedValues(
        string? sortBy,
        string? sortDirection)
    {
        // Act
        var result = _validator.Validate(new NotificationLookup
        {
            SortBy = sortBy,
            SortDirection = sortDirection,
        });

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("message")]
    [InlineData("toUserId")]
    [InlineData("from")]
    public void Validate_ShouldRejectUnknownSortBy(string sortBy)
    {
        // Act
        var result = _validator.Validate(new NotificationLookup
        {
            SortBy = sortBy,
        });

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(NotificationLookup.SortBy), error.PropertyName);
    }

    [Theory]
    [InlineData("newest")]
    [InlineData("descending")]
    public void Validate_ShouldRejectUnknownSortDirection(string sortDirection)
    {
        // Act
        var result = _validator.Validate(new NotificationLookup
        {
            SortBy = NotificationSortFields.Created,
            SortDirection = sortDirection,
        });

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(NotificationLookup.SortDirection), error.PropertyName);
    }
}
