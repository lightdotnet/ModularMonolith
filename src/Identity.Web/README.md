# Identity.Web

Razor Pages host with local username/password sign-in, optional Microsoft work/school account sign-in, and admin pages for users, roles, and permissions.

## Microsoft account configuration

The login page uses local username/password as the primary sign-in method. Microsoft login is an additional option. API username/password authentication is unchanged.

Microsoft login is enabled only when `Authentication:Microsoft:ClientId`, `Authentication:Microsoft:ClientSecret`, and at least one entry in `Authentication:Microsoft:AllowedTenantIds` are all set. Otherwise the login page shows the password form only and the host still starts.

Only work/school accounts are accepted. The OpenID Connect authority is `{Instance}organizations/v2.0` (`Instance` defaults to `https://login.microsoftonline.com/`), and a token is rejected unless its `tid` (tenant id) is in `Authentication:Microsoft:AllowedTenantIds` and its issuer is `{Instance}{tid}/v2.0`. Personal Microsoft accounts (Outlook.com, Hotmail) are not supported.

Register a web application in Microsoft Entra ID with **Supported account types** set to **Accounts in this organizational directory only** (single tenant) when one tenant signs in, or **Accounts in any organizational directory** (multitenant) when several tenants do. In both cases, list every tenant that may sign in in `AllowedTenantIds`.

A Microsoft user signs in when their Microsoft login is already linked to an active local user. A Microsoft user with no linked login is provisioned as a new local user only when the email's domain is in `Authentication:Microsoft:AllowedEmailDomains` and no local user already has that email; an existing local user with the same email is not linked automatically.

When Identity pages are hosted by `StarterKit.WebApi` (`src/StarterKit.WebApi`), add this redirect URI:

- `https://localhost:5001/signin-oidc`

When running the standalone `Identity.Web` host, add this redirect URI instead:

- `https://localhost:60160/signin-oidc`

In **Authentication > Add a platform > Web**, register every URI that will be used, exactly as shown above, including scheme, port and path. Use the actual HTTPS port from the host's `Properties/launchSettings.json` if it changes. The path comes from `Authentication:Microsoft:CallbackPath` (default `/signin-oidc`).

Every authorization request includes `prompt=select_account`, so Microsoft always shows the account picker. The URL therefore looks like:

```text
https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize?...&prompt=select_account
```

The **Sign out** and **Switch account** actions clear the local application and external sign-in cookies and return to the login page.

Configure the credentials with user secrets or environment variables; do not commit them to `appsettings*.json`:

```powershell
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<client-id>" --project src/Identity.Web/Identity.Web.csproj
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<client-secret>" --project src/Identity.Web/Identity.Web.csproj

# Use these commands when hosting the Identity pages in StarterKit.WebApi.
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<client-id>" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<client-secret>" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```

`AllowedTenantIds` and `AllowedEmailDomains` are arrays; set them in the active host's configuration, for example:

```json
{
  "Authentication": {
    "Microsoft": {
      "AllowedTenantIds": [ "<tenant-id>" ],
      "AllowedEmailDomains": [ "<email-domain>" ]
    }
  }
}
```

The value for `Authentication:Microsoft:ClientSecret` must be the **Secret value** copied when the client secret is created in Microsoft Entra ID. Do not use the secret's ID. If the value was committed or exposed, delete/revoke that secret in Entra ID, create a new one, and configure the new value with the command above. Restart the host after changing user secrets.

The host uses the existing Identity database configuration. Set `DbProvider` and `ConnectionStrings:DefaultConnection` in configuration as appropriate for the environment. The standalone host's `appsettings.json` ships a placeholder connection string to replace through user secrets or environment variables.

## Admin pages

The admin pages start at `/Admin`, which links to every section the signed-in user may open. Each page requires a permission from the Identity permission catalog:

| Area | Permissions |
|---|---|
| Users | `identity.users.view` to list; `identity.users.create`, `identity.users.update`, `identity.users.delete` for the matching actions |
| Roles | `identity.roles.view` to list; `identity.roles.manage` to create, edit, or delete |
| Permissions (role × permission matrix) | `identity.roles.view` to view; `identity.roles.manage` to change |

Sections are switched off in configuration under `IdentityWeb:Admin`. `Enabled` turns off every admin page; `Users`, `Roles`, and `Permissions` turn off one section each. A disabled page has no route (404) and its links are hidden:

```json
{
  "IdentityWeb": {
    "Admin": {
      "Enabled": true,
      "Users": true,
      "Roles": true,
      "Permissions": false
    }
  }
}
```

Changing a user's roles, claims, or active status, or a role's permissions, invalidates the affected users' sign-in cookies. `IdentityWeb:SecurityStampValidationInterval` (a `TimeSpan`, default `00:05:00`) sets how often a cookie is re-checked, so it is the longest time before such a change takes effect in an open session. A zero or negative value fails startup.

---
_Last synced: 2026-09-30_
