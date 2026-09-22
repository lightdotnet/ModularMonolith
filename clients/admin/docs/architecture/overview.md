# Client App Overview: admin

Internal admin console for the ModularMonolith starter template — the first and only client app
(`clients/` has no other subfolder). Layering, dependency direction, and design patterns are in
[architecture.md](./architecture.md); this file covers what the app *does* — functional areas,
routes, backend contract surface, and the auth flow.

## Functional Areas

- **Identity administration** (`/identity/users`, `/identity/roles`) — full CRUD against `Identity.Api`:
  users (with an Active Directory lookup on create, force-password-reset), roles (a permissions
  checklist plus a free-form "other claims" editor).
- **Notifications** (`/notifications` + a topbar bell + a Home inbox) — a permission-gated admin
  send/browse page, plus a live SignalR-backed unread-count/tab-filtered feed shared between the bell
  and the Home page.
- **Organization administration** (`/organization/{companies,departments,employees}`) against
  `Organization.Api` — company CRUD; a company-scoped department/team hierarchy (`OrgUnit`, unified
  via a `Type` discriminator) as a recursive tree with add/edit/move/delete + a read-only "View
  managers" dialog, plus a company-scoped Employee Levels panel; employee CRUD with a tabbed edit
  dialog (Details / Departments & Teams / Login) covering membership assignment (level, primary,
  `Current`/`Acting` status, manager flag) and creating or linking an Identity login.
- **Retail administration** (`/location`) against `Location.Api` — full CRUD for a global location
  hierarchy (tree) and location types (with configurable allowed parents and child support).
- **Inventory** (`/inventory`, `/inventory/valuation`) against `Inventory.Api` — two tabs sharing one
  view permission (`inventory.stock.view`): a read-only Stock Levels tab (filterable by
  product/location, plus an all-locations "Total on hand: X across N location(s)" summary banner shown
  when a product filter is active, independent of the location filter) and an Adjustments tab
  (filterable by product/location/source-order-id) with a `inventory.stock.manage`-gated "Record
  adjustment" action (dialog form with an optional unit-cost field, available to anyone who can manage
  stock). Cost data (average cost, total value, unit cost, value change) is shown only to viewers
  holding `inventory.stock.view_cost`; that permission also gates the separate Valuation page and, with
  `inventory.stock.revalue`, a "revalue cost" row action. Movement reasons (badge) include
  `PurchaseReturnOut` and `CostRevaluation`.
- **Transfers** (`/transfers`, `/transfers/[id]`) against `Transfers.Api` — stock transfers between
  locations, built two-phase: a create dialog makes the draft header and navigates to the detail page,
  where lines are added/edited. The detail page carries dispatch, receive, close, and cancel actions
  (each on its own permission) and per-line in-transit quantities; receiving records a partial receipt
  of what is still in transit. Cost columns render only with `inventory.stock.view_cost`.
- **Purchasing** (`/purchasing/{suppliers,orders,receipts,returns}`) against `Purchasing.Api`:
  - *Suppliers* — list/create/edit and activate/deactivate.
  - *Purchase orders* (`/purchasing/orders`, `/[id]`) — same create-draft-then-open-detail shape as
    Transfers; submit/resubmit for approval requires picking an approver from a
    `purchase_order/approvers` lookup, and the detail page links to the resulting request at
    `/approvals/requests/{id}`; withdraw/close/cancel; receiving requires a delivery-note reference.
  - *Goods receipts* (`/purchasing/receipts`, `/[id]`) — read-only list/detail of receipts recorded
    from a purchase order.
  - *Purchase returns* (`/purchasing/returns`, `/new`, `/[id]`) — a draft created from a posted goods
    receipt (`/new?receiptId=`) showing per-line returnable quantities; the detail page posts,
    cancels, and credits it. The cost-removed figure renders only with `inventory.stock.view_cost`.
- **Currency** (`/currency/{currencies,exchange-rates}`) against `Currency.Api`:
  - *Currencies* — a searchable, status-filterable list with create/edit (name, symbol, decimal
    places; the code is fixed once created) and activate/deactivate. The base currency carries a
    "Base" badge, cannot be deactivated, and its decimal places are locked.
  - *Exchange rates* — a paged rate history (filterable by currency and a date range, newest first),
    a "Latest rates" panel (the rate in effect per active foreign currency), and a record-rate dialog.
    History is append-only — no edit/delete; a correction is a newer rate. A rate is shown with up to
    8 decimal places (its own formatter, not the amount format) and the rate field rejects a comma
    decimal separator. The date filters are labelled UTC because the page is a Server Component and
    cannot know the viewer's time zone.
- **Catalog** (`/catalog`) against `Catalog.Api` — a Products tab (paginated/searchable data table
  filterable by category and status, create/edit, activate/deactivate, image management) and, gated
  by `catalog.categories.view`, a Categories tab (recursive tree with create/edit/move/delete). The
  product form's currency is a select fed by the *active* currencies (from the currency module),
  defaulting to the base currency; without `currency.currencies.view` (or if the lookup fails) it
  falls back to a text input pre-filled with the product's own currency. Prices render as
  `#0,000.00 CCY`.
- **Orders** (`/orders`) against `Orders.Api` — a draft-then-build workflow through one two-phase
  `OrderPanel` Dialog: Phase A creates the draft header (location; there is no currency field —
  amounts display in the order's own currency), Phase B (the same Dialog, remounted) builds it — an
  on-demand product search-and-add (no min-char gate, unlike this app's other async pickers), per-line
  quantity/sale-price editing, order-level discount and fee management, place/cancel. A line whose
  catalog price was converted from another currency shows a secondary "Catalog price X CCY × rate =
  unit price" line; a missing-exchange-rate error on add-line carries a hint pointing to the Exchange
  rates page. Payments take the order's currency read-only. Money renders as `#,##0.00 CCY` (two
  decimals, ISO code trailing). The order list (`OrdersDataTable`) is responsive: the full column set
  on desktop collapses into one stacked card-style block per row (status/location/total/date) below
  the `sm` breakpoint, filterable by location and status.
- **Approvals** (`/approvals`) against `Approval.Api` — a generic multi-level approval workflow: the
  caller's pending decisions and own requests, plus (for `approval.requests.view_all`) an admin
  view-all and a "Create test request" harness that builds an arbitrary-length approver chain.
- **Leave requests** (`/leave-requests`, `/leave-requests/[id]`) against `LeaveManagement.Api` —
  self-service submission/tracking of the caller's own requests (no permission gate, only a session);
  create/edit/delete restricted to the viewer's own requests in an editable status, each requiring a
  real department approver picked from a `GET leave_request/approvers` fetch. A caller with
  `leave.requests.manage` also gets an "All requests" tab (delete-only over every employee's
  requests). The detail page links out to `/approvals/requests/{id}` — decisions happen there, not
  here.
- **Home** (`/`) — a `ProfileSummaryCard` plus the notification inbox; a real Server Component
  resolving the session and fetching the caller's notifications.
- **Auth/session** — an encrypted, proactively-refreshed cookie session, reached via password login or
  a Microsoft PKCE relay (see Auth Flow).
- **Deploy resilience** — both `error.tsx` boundaries recognize deploy-induced stale-tab errors and
  show a health-probe-gated auto-reload notice instead of the generic error card.

## Structure

- **Router**: App Router, rooted at `src/app/`. No `pages/`.
- **Package manager**: pnpm. `pnpm-workspace.yaml` only configures build-script approval — a single
  independent app, not a workspace.
- **Module layout**: `src/modules/<domain>/<name>/` for everything except `src/features/home/` (the
  one holdout). Each folder: `api/` + `components/` + optional `types/`/`constants/`/`hooks/` + an
  `index.ts` barrel. See [architecture.md § Layering](./architecture.md#layering).
- **Data fetching**: server-only, hand-written per feature. Each `api/` has one consolidated
  `<name>.api.ts` wrapping every backend call, normalized through `lib/server/call-guard.ts`;
  `*-action.ts` Server Actions are one file per action (including read-only ones a Client Component
  needs). Whole-list reads happen in async Server Components; writes via a Server Action.
  `modules/notifications` is the one exception — a browser-direct SignalR WebSocket authenticated
  with a short-lived, hub-scoped token. See [architecture.md § Key Design Patterns](./architecture.md#key-design-patterns)
  for the API-layer, DataTable-consumption, and lazy-fetch patterns.
- **State management**: local component state + React Context, no global store. `*-data-table.tsx`
  components drive search/pagination through URL `searchParams`; dialogs use `useActionState` + a
  bumped remount `key`; the Approvals tabs and Leave requests tables render a server-fetched array
  with no owned pagination state. Persisted UI slices (sidebar, accent, theme) each get a Context
  provider; `NotificationsProvider` wraps live data + one shared SignalR connection.
- **Styling**: Tailwind CSS v4, CSS-first config in `src/app/globals.css`. No `tailwind.config.*`.

## Key Routes/Areas

| Route | Path | Notes |
|---|---|---|
| Home | `/` | Async Server Component — resolves the session (redirect to `/login` if absent), renders `ProfileSummaryCard` + `NotificationInbox` (initial page fetched server-side) |
| Profile | `/user-profile` | Account details, QR of the user id, roles/claims/permissions, session lifecycle card. Super-admin-only extras (`isSuperAdminUser`): a manual "Refresh now" action and a "Session tokens" card exposing the raw access/refresh token with copy + show/hide |
| Login | `/login` | Outside `(dashboard)` — no `AppShell`/session resolution. Password form + "Continue with Microsoft" |
| Microsoft login relay | `/login/microsoft/start`, `/login/microsoft/callback` | Route Handlers, not pages — see Auth Flow |
| Dashboard layout | `src/app/(dashboard)/layout.tsx` | `resolveSession()` → `SessionGate` wrapping `AppShell`. Sibling `error.tsx` (deploy-recovery branch) + `loading.tsx` (spinner) cascade to nested routes |
| Root layout | `src/app/layout.tsx` | Fonts, `ThemeProvider` → `AccentColorProvider` → `TooltipProvider`, `<AppToaster />`; owns `app/error.tsx` |
| Health probe | `/api/health` | `GET` → `204`, `force-dynamic`, no auth; polled by the deploy-recovery loop |
| Users | `/identity/users` | Gated `identity.users.*`. List/create/edit/delete/force-password |
| Roles | `/identity/roles` | Gated. Client-filtered list; create (name/description only); edit adds a permissions checklist + "Other claims" editor |
| Notifications | `/notifications` | Gated `notification.read`; "Send" gated `notification.send`. Status + recipient filter |
| Companies | `/organization/companies` | Gated. CRUD; edit works off row data (no on-open detail fetch) |
| Departments & Teams | `/organization/departments` | Gated. `?companyId=` picker + recursive tree + "Employee Levels" tab |
| Employees | `/organization/employees` | Gated. Search/paginate; tabbed edit dialog (Details / Departments & Teams / Login) |
| Locations | `/location` | Gated. Recursive tree + Location Types tab |
| Catalog | `/catalog` | Gated `catalog.products.view`. Products tab (search/paginate/filter by category+status, create/edit, activate/deactivate, images) + Categories tab (`catalog.categories.view`, recursive tree create/edit/move/delete) |
| Orders | `/orders` | Gated `orders.orders.view`; create/build/place/cancel gated `orders.orders.manage`. Two-phase `OrderPanel` Dialog (create draft → build), responsive list (card-style row on mobile) filterable by location/status |
| Inventory | `/inventory` | Gated `inventory.stock.view`. Two tabs: Stock Levels and Adjustments; cost columns need `inventory.stock.view_cost`; "Record adjustment" gated `inventory.stock.manage`; revalue action gated `inventory.stock.revalue` |
| Inventory valuation | `/inventory/valuation` | Gated `inventory.stock.view_cost`. Paged, product/location-filterable stock valuation with grand totals |
| Transfers | `/transfers`, `/transfers/[id]` | Gated `transfers.transfers.view`; create/dispatch/receive/close gated by the matching `transfers.transfers.*` permission |
| Suppliers | `/purchasing/suppliers` | Gated `purchasing.suppliers.view`; mutations `purchasing.suppliers.manage` |
| Purchase orders | `/purchasing/orders`, `/purchasing/orders/[id]` | Gated `purchasing.orders.view`; create `.create`, submit/withdraw `.submit`, close `.close` |
| Goods receipts | `/purchasing/receipts`, `/purchasing/receipts/[id]` | Gated `purchasing.receipts.view` |
| Purchase returns | `/purchasing/returns`, `/purchasing/returns/new`, `/purchasing/returns/[id]` | Gated `purchasing.returns.view`; `/new` requires `purchasing.returns.create` and a `?receiptId=`; credit gated `purchasing.returns.credit` |
| Currencies | `/currency/currencies` | Gated `currency.currencies.view`; create/edit/activate-deactivate gated `currency.currencies.manage` |
| Exchange rates | `/currency/exchange-rates` | Gated `currency.rates.view`; "Record rate" gated `currency.rates.manage` |
| Approvals | `/approvals` | Gated `approval.requests.view`; view-all panel + "Create test request" gated `approval.requests.view_all` |
| Leave requests | `/leave-requests`, `/leave-requests/[id]` | **No permission gate** — any session. `leave.requests.manage` unlocks an "All requests" tab + delete-any |

Every `page.tsx` is a one-line re-export from a feature/module barrel. `constants/nav-items.ts`
assembles `NAV_ITEMS` from each feature's own `NavItem`: `[home, Administration group, Organization
group, /approvals, /leave-requests, Retail group, Settings]`, where the Retail group holds Location,
Catalog, Orders, Inventory, Inventory Valuation, Transfers, the four Purchasing items (Suppliers,
Purchase orders, Goods receipts, Purchase returns), and the two Currency items (Currencies, Exchange
rates). `/administration`, `/organization`, `/retail`,
`/settings` have no `page.tsx` and 404 if followed; being ungated they still show in the sidebar and
⌘K palette.

## Backend Integration

Real, but partial. `lib/server/api-clients.ts` registers twelve backend clients — `Identity`,
`Notifications`, `Organization`, `Location`, `Approval`, `LeaveManagement`, `Catalog`, `Orders`,
`Inventory`, `Transfers`, `Purchasing`, `Currency` — each resolving its own `*_API_BASE_URL` env var
(the base URL owns its full path prefix; `http.ts` prepends nothing). `lib/server/backend-api.ts`'s
`createBackendApiClient(client)` factory produces twelve ready instances (`identityApi` … `locationApi`
… `leaveManagementApi` … `catalogApi` … `ordersApi` … `inventoryApi` … `transfersApi` …
`purchasingApi`, `currencyApi`); auth is attached by a request-handler pipeline (`bearerTokenHandler`
reads the ambient session), not a passed token. The twelve backends are logically separate modules
currently co-hosted in one process (`StarterKit.WebApi`).
Error handling, the envelope contract, and the permanent-vs-transient refresh-failure distinction are
covered in [architecture.md § Key Design Patterns](./architecture.md#key-design-patterns).

Endpoints this client consumes, by module:

- **auth** — `auth/token/get`, `auth/token/refresh`, `auth/token/external` (POST — exchanges a one-time
  PKCE code for a token; see Auth Flow) (`modules/identity/auth/api/token.api.ts`, explicit
  `client: Identity`); `auth/token/hub` (POST, authenticated — mints the short-lived hub-scoped token
  for the SignalR handshake, `modules/notifications/api/signalr.api.ts`). The Microsoft relay's
  browser-facing leg targets `Identity.Web` directly (`IDENTITY_WEB_BASE_URL`, not this client's
  `Identity` backend client) — see Auth Flow.
- **user-profile** — `user_profile` (GET), `user_profile/token/{list,revoke}`.
- **users** — `user/search`, `user` (GET-all / PUT / DELETE), get-by-id, create, force-password,
  `user/get_domain_user/{userName}` (AD lookup). `user/search` also backs the three on-demand
  user-search components.
- **roles** — `role` (GET-all / POST / PUT / DELETE), get-by-id.
- **permissions** — `permissions` (the definable-permission catalog for the Roles edit dialog).
- **notifications** — `notification` (admin GET/POST), `user_notification` (self-scoped
  GET/mark-read/count), plus a browser-direct WebSocket to `/signalr-hub` (`SIGNALR_HUB_URL`).
- **companies** — `company` (paged search / POST / PUT / DELETE), get-by-id.
- **departments** — `org_unit/company/{id}/tree`, `org_unit/{id}` (GET/PUT), `org_unit/{id}/move`,
  `org_unit` (POST), `org_unit/{id}` (DELETE), `org_unit/{id}/{employee,manager}`; `employee_level/company/{id}`
  + create/update/delete.
- **employees** — `employee/search`, `employee/{id}` (GET/PUT/DELETE), `employee` (POST),
  `employee/{id}/org_unit` (POST) + `/{orgUnitId}` (PUT/DELETE), `employee/{id}/login`
  (POST/PUT/DELETE). `searchEmployees` also resolves employee names for the Leave requests "All
  requests" tab.
- **locations** — `location/tree`, `location/{id}` (GET/PUT/DELETE), `location/{id}/move`,
  `location` (POST); `location_type` (GET/POST/PUT/DELETE).
- **catalog** — `category/tree`, `category/{id}` (GET/PUT/DELETE), `category/{id}/children`,
  `category/{id}/move` (PUT), `category` (POST); `product` (GET, paginated search), `product/{id}`
  (GET); `product/{id?}` (PUT — upserts: creates when `id` is omitted, updates when present, images
  included both ways), `product/{id}/{activate,deactivate}` (PUT), `product/{id}/image`
  (POST/DELETE).
- **orders** — `order` (GET search / POST create), `order/{id}` (GET), `order/{id}/line` (POST),
  `order/{id}/line/{lineId}/{quantity,sale_price}` (PUT), `order/{id}/line/{lineId}` (DELETE),
  `order/{id}/discount` (PUT/DELETE), `order/{id}/fee` (POST), `order/{id}/fee/{feeId}` (DELETE),
  `order/{id}/place` (PUT), `order/{id}/cancel` (PUT). `addOrderLine` is the one place a bigint id
  (`productId`) is coerced to a JSON number rather than sent as a string, since `Orders.Contracts`
  has no `[JsonNumberHandling]` relaxation for it. Line DTOs may carry the conversion snapshot
  (`catalogUnitPrice`, `catalogCurrency`, `appliedRate`, `rateEffectiveFrom`) when the catalog price was
  in another currency.
- **inventory** — `stock_level` (GET — paged search by product/location), `stock_level/total/{productId}`
  (GET — all-locations total quantity + location count for one product), `stock_level/valuation`
  (GET — valuation lines + grand totals; requires `inventory.stock.view_cost`), `stock_adjustment`
  (GET — paged search by product/location/source-order-id; POST — records a manual adjustment; the
  server always stamps `Reason: ManualAdjustment`, so the request body carries no `reason` field),
  `stock_adjustment/revaluation` (POST — cost revaluation; `inventory.stock.revalue`). Search/GET gated
  `inventory.stock.view`, the adjustment POST additionally `inventory.stock.manage`. Cost fields on the
  DTOs are null unless the caller has `inventory.stock.view_cost`. The POSTs coerce `productId` to a
  JSON number, the same bigint-id exception as Orders' `addOrderLine` (`Inventory.Contracts` has no
  `[JsonNumberHandling]` relaxation for it either).
- **transfers** — `stock_transfer` (GET paged search / POST create draft), `stock_transfer/{id}`
  (GET / PUT header update), `stock_transfer/{id}/line` (add) + `/line/{lineId}` (update/remove),
  `stock_transfer/{id}/{dispatch,close,cancel}` (PUT),
  `stock_transfer/{id}/receipt` (POST — carries a client request id; see architecture.md). Cost
  fields are null unless the caller has `inventory.stock.view_cost`.
- **suppliers** — `supplier` (GET paged search / POST), `supplier/{id}` (GET / update),
  `supplier/{id}/{activate,deactivate}`. A separate helper loads the first 200 suppliers for select
  options.
- **purchase-orders** — `purchase_order` (GET paged search / POST create draft), `purchase_order/{id}`
  (GET / update), `purchase_order/approvers` (GET — approver candidates),
  `purchase_order/{id}/line` (add) + `/line/{lineId}` (update/remove),
  `purchase_order/{id}/{submit,withdraw,close,cancel}`, `purchase_order/{id}/receipt` (POST — the
  delivery-note reference is the idempotency key).
- **goods-receipts** — `goods_receipt` (GET paged search), `goods_receipt/{id}` (GET).
- **purchase-returns** — `purchase_return` (GET paged search / POST create draft),
  `purchase_return/{id}` (GET / update), `purchase_return/{id}/{post,cancel,credit}`. A helper derives
  claimed quantities per receipt line from existing returns so the create form can show returnable
  quantities.
- **currencies** — `currency` (GET paged search by name/code and active flag / POST create),
  `currency/{code}` (GET / PUT update), `currency/{code}/{activate,deactivate}` (PUT). A separate helper
  (`getCurrencyOptions`) loads up to 100 currencies (optionally active only) for pickers and filters.
- **exchange-rates** — `exchange_rate` (GET paged history filtered by currency code and a from/to
  instant range / POST record a rate), `exchange_rate/latest` (GET — the rate in effect per active
  foreign currency, optionally as of a given instant). No update or delete endpoint is consumed.
- **approvals** — `modules/approvals/api/approvals.api.ts` (admin, `approval.requests.view_all`):
  `approval` (GET search / POST test request). `user-approvals.api.ts` (self-service, server-scoped
  by `UserApprovalController`): `approval/user` (GET / POST), `approval/user/{id}`,
  `approval/user/{id}/decide`.
- **leave-requests** — `leave_request/search`, `leave_request/{id}` (GET/PUT/DELETE),
  `leave_request/approvers`, `leave_request` (POST). `employeeId` search filter is honored
  server-side only for `leave.requests.manage`.

Every function returns a normalized `Result`/`ApiResponse` envelope via `call-guard.ts`. Gated pages
use `lib/server/require-permission.tsx`; `/leave-requests` deliberately does not (see architecture.md
§ Module/Route Boundaries). Permission-string constants live per-feature/module in
`constants/permissions.ts`, matching each backend module's own format (e.g.
`organization.companies.view`, `approval.requests.view_all`, `leave.requests.manage`,
`currency.rates.manage`). Cost visibility
across Inventory, Transfers, and Purchasing keys off the single Inventory permission
`inventory.stock.view_cost` (`INVENTORY_STOCK_PERMISSIONS.ViewCost`).

## Auth Flow

Cookie-based session, AES-256-GCM encrypted at rest (`TOKEN_ENCRYPTION_KEY`), with proactive refresh.
Two entry points converge on the same session-establishment step:

1. **Password login** — `LoginForm` submits to `loginAction`, which calls `getToken()` then
   `getCurrentUser()` (a profile failure doesn't block login), then `establishSession()`.
2. **Microsoft login** — `ExternalLoginLink` (a plain server-rendered `<a>`, a real top-level
   navigation) sends the browser to `/login/microsoft/start`, which generates a PKCE verifier/challenge
   pair (`lib/server/external-login-pkce.ts`), stores the verifier in a short-lived HttpOnly cookie, and
   redirects to `Identity.Web`'s `/Account/ExternalLoginStart` (`IDENTITY_WEB_BASE_URL` — a distinct,
   browser-reachable origin from the server-to-server `IDENTITY_API_BASE_URL`). After the backend's own
   relay completes, it redirects back to `/login/microsoft/callback`, which reads+clears the PKCE
   cookie, exchanges the code via `exchangeExternalLoginCode()` (`auth/token/external`), and also calls
   `establishSession()`. See `app/login/microsoft/{start,callback}/route.ts`.
3. **`establishSession()`** (`modules/identity/auth/api/establish-session.ts`, shared by both paths
   above) — **permissions and roles are decoded from the access-token JWT** (`lib/server/jwt.ts`),
   never trusted from the profile API; `claims` is the deduped union of both.
4. **Persist** — `persistSessionCookie()` reduces `SessionData` to the minimal `StoredSession`
   (tokens, expiries, profile, `refreshFailureCount`, `extraClaims` — `claims`/`permissions`/`roles`
   dropped, re-derived on read), encrypts it, and writes `admin_session` (`httpOnly`, `sameSite: lax`,
   `maxAge` from a hard 7-day `sessionExpiresAt`). Past `MAX_CHUNK_BYTES` it splits across numbered
   chunk cookies (`cookie-codec.ts`).
5. **Redirect** — both entry points honor a safe same-site return path (open-redirect guarded — the
   Microsoft path carries it through as `state`), else `/`. On failure, either path redirects to
   `/login?error=...`, rendered by the same `Alert` `LoginForm` already uses.
6. **`src/proxy.ts`** — a thin auth gate only: decrypt/validate/hydrate the cookie(s), enforce the
   7-day cap (missing/expired ⇒ `/login?redirect=<path>`, clearing every chunk name), and redirect
   away from the public auth paths when already authenticated. The public-path check is an explicit
   allow-list (`/login`, `/login/microsoft/start`, `/login/microsoft/callback`), not a single
   comparison. No token refresh or profile refetch anymore.
7. **`SessionGate`** (`components/layout/session-gate.tsx`, wrapping `AppShell`) drives freshness. On
   a hard navigation it calls `ensureFreshSessionAction({ refetchProfile: true })` behind a full-page
   overlay. That calls `refreshSessionIfNearExpiry()` (`REFRESH_LEAD_MS` = 5 min) which returns a
   `RefreshOutcome` (`skipped` / `success` / `failed{permanent}`); the action maps it to
   `fresh`/`updated`/`retry`/`degraded`. `retry` (transient) retries 2× then polls a
   `SessionUnreachableOverlay` every 5s; `updated` triggers `router.refresh()`; `degraded`
   (permanent 401/400) increments `refreshFailureCount` and force-logs-out at `MAX_REFRESH_FAILURES`
   (3). After settling, a silent 60s interval keeps a long-open session fresh.
8. **Logout** — `logoutAction` deletes every session cookie and redirects to `/login?redirect=<path>`
   (same guard). Session expiry and explicit logout both funnel through the same redirect pattern.
9. **`getSession()`** reads and decrypts the cookie (no fetch); `resolveSession()` is a thin
   passthrough used by the dashboard layout and every gated page.

**SignalR handshake token.** `getSignalRTokenAction()` (`modules/notifications/api/get-signalr-token-action.ts`)
first calls `refreshSessionIfNearExpiry()` so the session bearer is valid (the mint call is
authenticated and `proxy.ts` skips `/api` paths), then calls `getHubToken()` →
`POST auth/token/hub` for a **dedicated hub-audience-scoped token** (~120s, `uid`+`jti` only) — not
the session access token. It returns that token plus the server-resolved `SIGNALR_HUB_URL`.
`use-notifications.ts` passes an `accessTokenFactory` that re-invokes the action on every (re)connect,
so `withAutomaticReconnect()` always gets a fresh short-lived token that is rejected on `/api`. The
only other places a token reaches the browser are the short-lived PKCE `code_verifier` cookie (never
an access token) used by the Microsoft login relay above, and the super-admin-only "Session tokens"
card on `/user-profile` (`isSuperAdminUser` gate), which renders the raw session access/refresh token
for inspection. `token-cipher.ts` uses Node's `crypto` and `proxy.ts` has no explicit runtime pin — see
[architecture.md § Known Risks](./architecture.md#known-architectural-risks--debt).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
