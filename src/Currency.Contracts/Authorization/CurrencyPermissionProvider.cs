using Light.AspNetCore.Authorization;

namespace StarterKit.Currencies.Contracts.Authorization;

public class CurrencyPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            CurrencyPermissions.Currencies.View,
            "View Currencies",
            CurrencyPermissions.Group);

        yield return new(
            CurrencyPermissions.Currencies.Manage,
            "Manage Currencies",
            CurrencyPermissions.Group);

        yield return new(
            CurrencyPermissions.Rates.View,
            "View Exchange Rates",
            CurrencyPermissions.Group);

        yield return new(
            CurrencyPermissions.Rates.Manage,
            "Manage Exchange Rates",
            CurrencyPermissions.Group);
    }
}
