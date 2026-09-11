# Identity.Web

Razor Pages host with local username/password sign-in and optional Microsoft work/school account sign-in.

## Microsoft account configuration

Register a web application in Microsoft Entra ID with **Supported account types** set to **Accounts in any organizational directory and personal Microsoft accounts**. Work/school Microsoft accounts remain supported by default. Personal accounts such as Outlook.com and Hotmail are rejected by default and can be enabled with `Authentication:Microsoft:AllowConsumerAccounts=true`.

The login page uses local username/password as the primary sign-in method. Microsoft login is an additional option. API username/password authentication is unchanged.

When Identity pages are hosted by `StarterKit.WebApi`, add this redirect URI:

- `https://localhost:5001/signin-oidc`

When running the standalone `Identity.Web` host, add this redirect URI instead:

- `https://localhost:60160/signin-oidc`

In **Authentication > Add a platform > Web**, register every URI that will be used, exactly as shown above, including scheme, port and path. Use the actual HTTPS port from the host's `Properties/launchSettings.json` if it changes. Configure the credentials with user secrets or environment variables; do not commit them to `appsettings*.json`:

The application uses the Microsoft `common` endpoint so both personal and work/school accounts are supported. The final authorization URL is forced to include `prompt=select_account` and an empty `login_hint`. The URL should therefore contain:

```text
https://login.microsoftonline.com/common/oauth2/v2.0/authorize?...&prompt=select_account&login_hint=
```

The **Sign out** and **Switch account** actions also clear local authentication cookies before starting a new authorization request. If the browser still shows a `consumers` URL or the URL does not contain `prompt=select_account`, stop the running process completely and restart the current `Identity.Web` project; that request came from an older process or configuration.

```powershell
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<client-id>" --project src/Identity.Web/Identity.Web.csproj
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<client-secret>" --project src/Identity.Web/Identity.Web.csproj

# Use these commands when hosting the Identity pages in StarterKit.WebApi.
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<client-id>" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<client-secret>" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```

The value for `Authentication:Microsoft:ClientSecret` must be the **Secret value** copied when the client secret is created in Microsoft Entra ID. Do not use the secret's ID. If the value was committed or exposed, delete/revoke that secret in Entra ID, create a new one, and configure the new value with the command above. Restart the host after changing user secrets.

The host uses the existing Identity database configuration. Set `DbProvider` and `ConnectionStrings:Identity` in configuration as appropriate for the environment. The default sample points to the local SQL Server database used by the starter kit.

To allow personal Microsoft accounts for a demo, set this configuration value in the active host:

```json
{
  "Authentication": {
	"Microsoft": {
	  "AllowConsumerAccounts": true
	}
  }
}
```
