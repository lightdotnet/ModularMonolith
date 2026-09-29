using StarterKit.Shared.Constants;
using Xunit;

namespace Framework.Tests.Shared.Constants;

public class CronTimeConstantsTests
{
    [Fact]
    public void EveryMins_ShouldFormatCorrectly()
    {
        // Act & Assert
        Assert.Equal("*/5 * * * *", CronTimeConstants.EveryMins(5));
    }

    [Fact]
    public void EveryHours_ShouldFormatCorrectly()
    {
        // Act & Assert
        Assert.Equal("0 */3 * * *", CronTimeConstants.EveryHours(3));
    }

    [Fact]
    public void EveryDayAt_ShouldFormatCorrectly()
    {
        // Act & Assert
        Assert.Equal("00 17 * * *", CronTimeConstants.EveryDayAt(17));
    }

    [Fact]
    public void StaticFields_ShouldMatchExpectedCronExpressions()
    {
        // Assert
        Assert.Equal("*/5 * * * *", CronTimeConstants.Every5Minutes);
        Assert.Equal("0 * * * *", CronTimeConstants.Every1Hour);
        Assert.Equal("00 17 * * *", CronTimeConstants.EveryDayAt0Utc7);
    }
}
