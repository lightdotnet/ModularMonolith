using System.Reflection;
using Currency.Tests.TestSupport;
using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using StarterKit.Currencies.Api.Controllers;
using StarterKit.Currencies.Contracts.Authorization;

namespace Currency.Tests.Controllers;

/// <summary>
/// The <c>[MustHavePermission]</c> gates are authorization middleware, so they are asserted by reflection on
/// the attributes rather than by running the pipeline.
/// </summary>
public class CurrencyControllersTests
{
    private static string[] PermissionsOf(
        Type controller,
        string action) =>
        controller
            .GetMethod(action)!
            .GetCustomAttributes<MustHavePermissionAttribute>()
            .Select(x => x.Policy!)
            .ToArray();

    private static string[] ClassPermissionsOf(Type controller) =>
        controller
            .GetCustomAttributes<MustHavePermissionAttribute>()
            .Select(x => x.Policy!)
            .ToArray();

    [Fact]
    public void CurrencyController_ShouldRequireTheViewPermission_ByDefault()
    {
        Assert.Contains(CurrencyPermissions.Currencies.View, ClassPermissionsOf(typeof(CurrencyController)));
    }

    [Fact]
    public void ExchangeRateController_ShouldRequireTheRatesViewPermission_ByDefault()
    {
        Assert.Contains(CurrencyPermissions.Rates.View, ClassPermissionsOf(typeof(ExchangeRateController)));
    }

    [Theory]
    [InlineData(nameof(CurrencyController.PostAsync))]
    [InlineData(nameof(CurrencyController.PutAsync))]
    [InlineData(nameof(CurrencyController.ActivateAsync))]
    [InlineData(nameof(CurrencyController.DeactivateAsync))]
    public void CurrencyWriteActions_ShouldRequireTheManagePermission(string action)
    {
        Assert.Contains(CurrencyPermissions.Currencies.Manage, PermissionsOf(typeof(CurrencyController), action));
    }

    [Fact]
    public void RecordingARate_ShouldRequireTheRatesManagePermission()
    {
        Assert.Contains(
            CurrencyPermissions.Rates.Manage,
            PermissionsOf(typeof(ExchangeRateController), nameof(ExchangeRateController.PostAsync)));
    }

    [Fact]
    public void ReadActions_ShouldNotDemandTheManagePermission()
    {
        Assert.Empty(PermissionsOf(typeof(CurrencyController), nameof(CurrencyController.SearchAsync)));
        Assert.Empty(PermissionsOf(typeof(CurrencyController), nameof(CurrencyController.GetAsync)));
        Assert.Empty(PermissionsOf(typeof(ExchangeRateController), nameof(ExchangeRateController.SearchAsync)));
        Assert.Empty(PermissionsOf(typeof(ExchangeRateController), nameof(ExchangeRateController.GetLatestAsync)));
    }

    [Fact]
    public void TheRateHistory_ShouldExposeNoUpdateOrDeleteEndpoint()
    {
        var verbs = typeof(ExchangeRateController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .ToList();

        Assert.DoesNotContain("PUT", verbs);
        Assert.DoesNotContain("DELETE", verbs);
        Assert.DoesNotContain("PATCH", verbs);
    }

    [Fact]
    public void ExchangeRateController_ShouldRequireAnAuthenticatedUserId()
    {
        Assert.Throws<ArgumentNullException>(() => new ExchangeRateController(new FakeCurrentUser { UserId = null }));
    }

    [Fact]
    public void PermissionProvider_ShouldRegisterEveryCurrencyPermissionUnderTheGroup()
    {
        var keys = new CurrencyPermissionProvider().Define().Select(x => x.Name).ToList();

        Assert.Equal(4, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Contains("currency.currencies.view", keys);
        Assert.Contains("currency.currencies.manage", keys);
        Assert.Contains("currency.rates.view", keys);
        Assert.Contains("currency.rates.manage", keys);
        Assert.All(keys, x => Assert.StartsWith("currency.", x));
    }
}
