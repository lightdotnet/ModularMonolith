using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Web;
using Xunit;

namespace Identity.Tests.Web;

public class DependencyInjectionTests
{
    private static SecurityStampValidatorOptions ResolveSecurityStampOptions(string? interval)
    {
        var settings = new Dictionary<string, string?>();
        if (interval is not null)
            settings["IdentityWeb:SecurityStampValidationInterval"] = interval;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddIdentityWeb(configuration);

        using var provider = services.BuildServiceProvider();
        return provider
            .GetRequiredService<IOptions<SecurityStampValidatorOptions>>()
            .Value;
    }

    [Fact]
    public void AddIdentityWeb_WhenIntervalConfigured_ShouldApplyIt()
    {
        // Act
        var options = ResolveSecurityStampOptions("00:01:30");

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(90), options.ValidationInterval);
    }

    [Fact]
    public void AddIdentityWeb_WhenIntervalMissing_ShouldDefaultToFiveMinutes()
    {
        // Act
        var options = ResolveSecurityStampOptions(interval: null);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), options.ValidationInterval);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:01:00")]
    public void AddIdentityWeb_WhenIntervalNotPositive_ShouldFailValidation(string interval)
    {
        // Act & Assert
        Assert.Throws<OptionsValidationException>(() => ResolveSecurityStampOptions(interval));
    }
}
