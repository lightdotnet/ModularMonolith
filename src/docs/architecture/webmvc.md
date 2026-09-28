# Host: StarterKit.WebMvc

`src/StarterKit.WebMvc` is a separate, server-rendered web host (ASP.NET Core MVC controllers + Razor
Pages). It lives in the backend solution but is **not a module host**: it is an HTTP client of
`StarterKit.WebApi`, integrating with the backend exactly like `clients/admin` does — see
[docs/integration.md](../../../docs/integration.md). This file covers its structure and runtime
conventions; the step-by-step rules for adding a screen live in the
[razor-web skill](../../../.claude/skills/razor-web/SKILL.md) and the
[razor-web-developer agent](../../../.claude/agents/razor-web-developer.md).

## Position in the Solution

- References only `<Module>.Contracts` projects (today `Identity.Contracts` and
  `Notifications.Contracts`) for DTOs and permission constants — never a module's `.Api`,
  `Identity.Web`, `Infrastructure`, or `Persistence`, and it never dispatches mediator commands. The edges are recorded in [dependency-graph.md](dependency-graph.md).
- Runs as its own process (`src/StarterKit.WebMvc/Program.cs`) with its own cookie session; it has no
  database and no module assemblies loaded.
- Every backend response is the `Result` envelope produced by `ApiControllerBase.Ok()`; the host
  unwraps it in one place (see § Backend Seam).

## Project Shape

| Folder | Role |
|---|---|
| `Services/<Module>/` | The backend seam: one `I<Resource>Client` interface per backend REST controller (one method per endpoint) plus its `HttpClient` implementation. |
| `Services/Http/` | Shared transport: `ApiClientBase`, the envelope reader `ApiResponseReader`, `BearerTokenHandler`, `ApiResult`, `ApiAuthorizationException`. |
| `Authentication/` | Cookie session (ticket, cookie events, principal built from the JWT), Microsoft-login PKCE state, safe return URLs. |
| `Authorization/` | `IPermissionChecker`, the `perm:` policy provider, `[HasPermission]`. |
| `Controllers/` + `Views/` | MVC screens — used for action/partial-centric flows and JSON/partial endpoints (e.g. the notifications admin and inbox). |
| `Pages/` | Razor Pages screens — used for page-centric CRUD (e.g. Identity users/roles) and the `Account/*` sign-in pages. |
| `Web/` | Controller/PageModel base classes (`WebControllerBase`, `WebPageModel`), result → flash/ModelState handling, data-table query/paging types, navigation menu. |
| `TagHelpers/`, `ViewComponents/`, `Views/Shared/` | The shared UI kit (see § UI Composition). |
| `Infrastructure/` | DI composition, option types, security headers, sign-in rate limiting, API 401/403 handling middleware. |
| `wwwroot/` | Site CSS and vanilla ES-module JS; third-party libraries restored by LibMan into `wwwroot/lib/`. |

## Backend Seam

- Each backend module gets a named `HttpClient` whose base address is `Api:<Module>:BaseUrl` (an
  absolute URL that owns the full path/version prefix, e.g. `.../api/v1/`); both base URLs are
  validated on start.
- Typed clients derive from `ApiClientBase`, which builds the request, sends it, and returns an
  `ApiResult`/`ApiResult<T>` — transport failures (network, timeout) come back as a failed result, not
  an exception.
- `ApiResponseReader` is the only place that interprets the backend envelope (success envelope,
  vendor exception envelope with a numeric code and flattened validation message, or
  `ValidationProblemDetails`), mapping validation failures to per-field errors for `ModelState`.
- `BearerTokenHandler` attaches the access token from the current cookie session (per-call options
  allow anonymous calls or an explicit token while a session is being established).
- A backend 401/403 is raised as `ApiAuthorizationException` and turned by
  `ApiAuthorizationExceptionMiddleware` into sign-out → login (401) or the access-denied page (403).
- Controllers and PageModels depend only on the `I<Resource>Client` interfaces, which carry no MVC
  types, so an in-process implementation can later replace the HTTP one without touching screens.

## Authentication and Session

The session model deliberately mirrors the `clients/admin` BFF (same lifetimes, same refresh rules):

- **Cookie session** — scheme `WebMvcSession`, cookie `webmvc_session`, httpOnly, `SameSite=Lax`,
  Secure outside Development, encrypted by ASP.NET Core Data Protection. It stores the backend access
  and refresh tokens in the authentication ticket; the principal's roles/permissions are decoded from
  the access-token JWT and the profile comes from `user_profile`.
- **Password / AD sign-in** — `/Account/Login` calls `auth/token/get` server-to-server.
- **Refresh** — `SessionCookieEvents.ValidatePrincipal` enforces a 7-day hard cap from sign-in (not
  extended by refresh) and rotates the access token via `auth/token/refresh` within 5 minutes of its
  expiry, rebuilding the principal from the new JWT. A permanent refresh failure never rewrites the
  cookie (multi-tab rotation race); the session is rejected only after repeated permanent failures,
  and a transient failure never counts.
- **Microsoft sign-in** — `/Account/ExternalLogin` creates a PKCE pair, keeps the verifier in a
  short-lived Data-Protection-encrypted cookie scoped to the callback path, and redirects the browser
  to `Identity.Web`'s `/Account/ExternalLoginStart` relay; `/Account/ExternalCallback` exchanges the
  one-time code via `auth/token/external`. The callback URL must be listed exactly in the backend's
  `ExternalLoginRelay:AllowedRedirectUris` — see
  [modules/Identity.md § External Login (OIDC)](modules/Identity.md#external-login-oidc).
- **SignalR** — the browser connects directly to the backend hub. `GET /notifications/hub-token`
  returns a short-lived hub-audience token minted by `auth/token/hub` plus the configured hub URL; the
  session JWT itself never reaches the browser.

## Authorization

- A fallback policy requires an authenticated `WebMvcSession` user everywhere; sign-in pages opt out
  with `[AllowAnonymous]`.
- `[HasPermission(<Module>Permissions.X)]` on a controller, action, or PageModel resolves a
  `perm:<permission>` policy on the fly; markup is gated with the `asp-permission` tag helper, and
  data-table columns/actions take a `permission` attribute.
- Permissions are the JWT's permission claims. `Authorization:SuperAdminUserNames` lists usernames that
  bypass every check (empty by default; Development lists `super`). The backend still enforces its own
  permissions on every call.

## UI Composition

- Bootstrap 5.3 + Bootstrap Icons, `aspnet-client-validation`, and the `@microsoft/signalr` browser
  client, restored from `libman.json` on build (`Microsoft.Web.LibraryManager.Build`). No jQuery and no
  SPA framework; page behaviour is vanilla ES modules under `wwwroot/js/` (data table, confirm dialog,
  toasts, local date-time rendering, a fetch helper that sends the antiforgery header).
- Shared tag helpers: `<data-table>` (with `dt-column`/`dt-action`/`dt-filters`), form field/select/
  check/submit helpers, display helpers (numbers, dates, status badges, empty state), `page-header`,
  `confirm-button`, and `asp-permission`. View components render the sidebar navigation, toasts, and
  the notification bell.
- **Data tables are server-rendered end to end.** Columns are declared once in the table's partial
  view; the first render and every refresh (search, sort, paging, filters) request the same handler,
  which returns that partial when the `X-DataTable` header names the table. Sort and page state travel
  in the query string and are passed to the backend's paged endpoints.
- Screens target Desktop and Mobile; tables collapse to cards below the `md` breakpoint.

## Security Constraints

- **Content-Security-Policy** allows scripts and styles from the host's own origin only — views must
  not contain inline `<script>` blocks, inline event handlers, or `style` attributes (a
  non-executing `application/json` data island is allowed). `connect-src` adds the SignalR hub origin
  and `form-action` adds the `Identity.Web` origin; both come from configuration. Also sets
  `X-Content-Type-Options`, `Referrer-Policy`, and `X-Frame-Options: DENY`.
- Antiforgery validation is a global filter; `fetch` callers send the token in the
  `RequestVerificationToken` header.
- Sign-in endpoints (password POST, Microsoft login start) are rate-limited per client IP
  (`RateLimiting:SignIn`).
- Forwarded headers are honoured only from loopback and the configured `ForwardedHeaders` proxies and
  networks; this feeds HTTPS redirection, Secure cookies, redirect URIs, and the per-IP rate limit.
- Outside Development, Data Protection keys must be persisted to `DataProtection:KeysPath` (shared by
  every instance) and should be protected with a certificate; otherwise every restart invalidates all
  session, antiforgery, TempData, and PKCE cookies.

## Configuration

Sections in `src/StarterKit.WebMvc/appsettings.json` (Development values in
`appsettings.Development.json`):

| Section | Purpose |
|---|---|
| `Api:Identity:BaseUrl`, `Api:Notifications:BaseUrl` | Server-to-server base URL per backend module, including the `api/v1/` prefix. Required. |
| `IdentityWeb:BaseUrl` | Browser-reachable origin of `Identity.Web` for the Microsoft relay. |
| `SignalR:HubUrl` | Absolute hub URL handed to the browser. |
| `ExternalLogin:EnableMicrosoft` | Shows/enables the Microsoft sign-in path. |
| `Authorization:SuperAdminUserNames` | Permission-bypass usernames. |
| `DataProtection` | Key directory and key-encryption certificate (required outside Development). |
| `ForwardedHeaders` | Trusted proxies/networks. |
| `RateLimiting:SignIn` | Sign-in attempts per IP per window. |

How to run it alongside `StarterKit.WebApi`, and which WebApi settings it needs, is in
[../conventions/development-guide.md § Running the WebMvc host](../conventions/development-guide.md#running-the-webmvc-host).

## Known Debt

Tracked in [../known-debt.md](../known-debt.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-28_
