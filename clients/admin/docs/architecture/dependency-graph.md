# Dependency Graph: admin

## Package References

The full dependency/devDependency list with exact versions lives in `clients/admin/package.json` — short enough to read directly rather than duplicating a version table here that would just drift out of sync. Grouped by purpose:

- **Framework/runtime**: `next`, `react`, `react-dom`.
- **UI primitives**: `radix-ui` (unified Radix primitives, base for `components/ui/*`), `class-variance-authority` (`cva`), `clsx` + `tailwind-merge` (combined in `lib/shared/utils.ts`'s `cn()`), `lucide-react` (icon set), `cmdk` (filterable-list primitive behind `components/ui/command.tsx`, the base of `components/ui/combobox.tsx`).
- **Theming/toast**: `next-themes` (light/dark/system switching; also drives `components/toast/toaster.tsx`'s toast theme), `sonner` (toast notifications, wrapped by `components/toast/`, never imported directly by feature/module code), `tw-animate-css` (animation utility classes).
- **Feature-specific**: `qrcode` (QR code on `/user-profile`), `@microsoft/signalr` (real-time push client, used only by `modules/notifications/hooks/use-notifications.ts`).
- **Positioning/virtualization**: `@floating-ui/react` (the Command Palette — reachable in the app via the topbar `SearchBox` — plus the surviving `components/foundation/{use-listbox,floating-overlay}`), `@tanstack/react-virtual` (headless list virtualization behind `components/foundation/use-virtual-list.ts`, used by the DataTable's `"virtualized"`/`"infinite"` modes and the Command Palette's result list).
- **Server-only guard**: `server-only` — compile-time marker forcing a build error if a `lib/server/*` module is ever imported into a Client Component; present in all 18 files under `lib/server/` (the 17 flat files plus `http-handlers/bearer-token-handler.ts`).
- **Styling toolchain** (devDependencies): `tailwindcss` + `@tailwindcss/postcss` (Tailwind v4), `shadcn` (CLI that generated `components/ui/*`; also imported at runtime for `shadcn/tailwind.css`), `prettier` + `prettier-plugin-tailwindcss` (no config file/`format` script found — see [coding-conventions.md](../conventions/coding-conventions.md)).
- **Tooling** (devDependencies): `eslint` + `eslint-config-next` (pinned to match `next`'s version), `typescript`, `@types/*`.

Package manager: pnpm (`pnpm-lock.yaml`). `pnpm-workspace.yaml` exists but only holds build-script approval (`sharp`, `unrs-resolver`), dependency `overrides`, and minimum-release-age exclusions — not a multi-package workspace. Node is pinned via `.nvmrc` + `package.json` `engines` (`node >=26.8.2`). `package.json` is the source of truth for exact versions; ordinary version bumps in the course of dependency updates are not tracked in prose here or in `architecture.md`.

## Module Layout (for import-path purposes)

Two top-level roots hold feature/module code:

- `src/features/home/` — the one feature that stayed here; every other feature moved under `src/modules/`.
- `src/modules/<domain>/<name>/` — `identity/{auth,user-profile,users,roles}`, `notifications` (flat, no further nesting), `organization/{companies,departments,employees}`, `approvals`, `leave-requests`, and the flat `location`, `catalog`, `orders`, `inventory`, `transfers`; `purchasing/{suppliers,purchase-orders,goods-receipts,purchase-returns,common}` and `currency/{currencies,exchange-rates,common}` nest one level, with `common` holding the shared permissions/limits/helpers of that domain rather than a feature. Nothing lives under `src/features/<name>/` except `home`.

## Circular References

None found among internal module imports. `components/ui/*` is not an absolutely strict leaf layer: `components/ui/dialog.tsx` and `components/ui/popover.tsx` both import `components/foundation/portal-container.ts`, a deliberate, narrow exception (a dependency-free React Context) that lets a Popover portal into an open Dialog's own DOM node rather than `document.body` (see [architecture.md](./architecture.md#key-design-patterns)). `components/ui/*` never imports from `components/layout/*`, `components/theme/*`, or a feature/module, and no cycle results — `components/foundation/*` doesn't import back from `components/ui/*`.

**Barrel-bypass exceptions to the "cross-feature/cross-module imports go through `index.ts`" rule** — the set below was last re-verified for the modules that existed at the previous sync plus the `catalog` product-search picker; the newer `transfers`, `inventory`, and `purchasing` modules also import `modules/location/*` files directly (API/type files) and were not audited edge-by-edge against the `location` barrel. Two distinct reasons account for nearly all of the verified ones:

**(A) The target item is genuinely not exported by that module's barrel** — usually because it's a Server Action (this app's barrels never re-export `*-action.ts` files) or a small presentational component the barrel never needed to expose:

| Importer | Imports directly | Why not in the barrel |
|---|---|---|
| `components/layout/user-menu.tsx` | `modules/identity/auth/api/logout-action` | Server Action, never barrel-exported |
| `components/layout/session-gate.tsx` | `modules/identity/auth/api/ensure-fresh-session-action` | Server Action, never barrel-exported |
| `modules/identity/users/components/edit-user-dialog.tsx` | `modules/identity/roles/api/get-all-roles-action` | Server Action, never barrel-exported |
| `modules/identity/users/components/users-data-table.tsx` | `modules/identity/user-profile/components/user-status-badge` | Component not re-exported by the `user-profile` barrel |
| `modules/notifications/components/user-select.tsx` | `modules/identity/users/api/search-users-action` | Server Action, never barrel-exported |
| `modules/organization/employees/components/user-select.tsx` | `modules/identity/users/api/search-users-action` | Same as above — second duplicate consumer |
| `modules/approvals/components/approver-select.tsx` | `modules/identity/users/api/search-users-action` | Same as above — third duplicate consumer |
| `product-select.tsx` in `modules/orders`, `modules/inventory`, `modules/transfers`, `modules/purchasing/purchase-orders` | `modules/catalog/api/search-products-action` | Server Action, never barrel-exported by `catalog` |
| `modules/organization/employees/components/employee-login-tab.tsx` | `modules/identity/users/api/get-user-detail-action` | Server Action, never barrel-exported |
| `modules/organization/departments/components/company-filter.tsx` | `modules/organization/companies/components/company-select` | Component not re-exported by the `companies` barrel |
| `modules/organization/employees/components/create-employee-dialog.tsx` | `modules/organization/companies/components/company-select` | Same as above |
| `modules/organization/departments/api/get-org-unit-managers-action.ts`, `.../components/view-org-unit-managers-dialog.tsx`, `.../api/org-units.api.ts` | `modules/organization/employees`'s barrel, `EmployeeDto` type only | Not a bypass — this one genuinely goes *through* the barrel; listed here only because it's the counterpart of the reversed dependency below |

**(B) The item *is* exported by the target barrel, but the importer is a Client Component avoiding the barrel anyway**, because that barrel also re-exports an async Server Component (or other server-only code) that would otherwise get pulled into the client bundle:

| Importer (all `"use client"` except `nav-items.ts`, which is imported by the client-side `Sidebar`) | Imports directly | Also exported (unused) via the target barrel |
|---|---|---|
| `constants/nav-items.ts` | Every nav-bearing module's `constants/nav-item.ts` (21 files: `features/home`, `identity/{users,roles}`, `notifications`, `organization/{companies,departments,employees}`, `location`, `catalog`, `orders`, `inventory` (two: `nav-item` + `valuation-nav-item`), `transfers`, `purchasing/{suppliers,purchase-orders,goods-receipts,purchase-returns}`, `currency/{currencies,exchange-rates}`, `approvals`, `leave-requests`) | Each module's `NavItem` constant, plus that barrel's Server Component |
| `components/layout/topbar.tsx` | `modules/notifications/components/notification-bell` | `NotificationBell` is in the `notifications` barrel |
| `modules/organization/employees/components/edit-employee-dialog.tsx` | `modules/organization/departments/api/{get-org-unit-tree-action,get-employee-levels-action}` | Both are in the `departments` barrel |

Two Server-Component (non-client) call sites import a barrel-exported function directly anyway, for consistency rather than necessity (no client-bundle risk, since neither is `"use client"`): `modules/organization/employees/components/employees-page.tsx` and `modules/organization/departments/components/departments-page.tsx` both call `searchCompanies` from `modules/organization/companies/api/companies.api` directly rather than via the `companies` barrel (which does export it); `features/home/components/home-page.tsx` similarly imports `modules/notifications/components/notification-inbox`'s `NotificationInbox` directly even though the `notifications` barrel also re-exports it (its sibling import, `get-my-notifications-action`, falls under reason (A) — that one genuinely isn't barrel-exported).

All of the above are one-way, file-level edges with no reverse import back from the target into the importer — no cycle results from any of them.

Two more, lower-level exceptions exist at the `lib/server` tier, both a reversal of the usual `feature/module -> lib/server/*` direction: `lib/server/refresh-session.ts` imports `modules/identity/auth/api/token.api.ts` directly (bypassing the `@/modules/identity/auth` barrel, which does export `refreshToken`), and `lib/server/refetch-profile.ts` imports `modules/identity/user-profile/api/user-profile.api.ts` directly (bypassing the `@/modules/identity/user-profile` barrel, which does export `getCurrentUser`) — both are called from `modules/identity/auth/api/ensure-fresh-session-action.ts`, the Server Action `components/layout/session-gate.tsx` drives (see [architecture.md](./architecture.md#key-design-patterns)). This is a change from the original design, where `src/proxy.ts` itself held the one direct feature-api-file import at the routing layer (`getCurrentUser`) — that import moved into `refetch-profile.ts` once the refresh/profile-freshness logic moved out of middleware. `lib/server/require-permission.tsx` does **not** share this reversal — it imports `identity/user-profile`'s barrel the same way ordinary page code does.

A genuine, if narrow, **sibling-module type dependency** exists between `organization/departments` and `organization/employees`: `departments/api/org-units.api.ts`, `departments/api/get-org-unit-managers-action.ts`, and `departments/components/view-org-unit-managers-dialog.tsx` all `import type { EmployeeDto } from "@/modules/organization/employees"` (the barrel, properly), since `getOrgUnitEmployees`/`getOrgUnitManagers` both return employee records. The reverse direction also holds — `organization/employees` imports several of `organization/departments`'s types and Server Actions directly (`types/{org-unit,employee-level}`, `api/{get-org-unit-tree-action,get-employee-levels-action}`, both listed under (B) above). Because the `departments -> employees` edge is `import type` only, it's erased at compile time and creates no runtime cycle with the `employees -> departments` edge — TypeScript/the bundler never has to resolve both directions of an actual module graph simultaneously.

The newer modules add cross-module edges that are one-way and cycle-free by construction: `transfers` reads `INVENTORY_STOCK_PERMISSIONS` from the `inventory` barrel (cost-visibility gate), the Purchasing list/detail pages read `getSupplierOptions` from the `suppliers` barrel and `getGoodsReceiptById` from the `goods-receipts` barrel, and both `catalog` (the products page, for the product form's currency select) and `currency/exchange-rates` (the rates page) read `getCurrencyOptions` from the `currency/currencies` barrel; `purchasing/common` and `currency/common` are imported by their sibling modules and import none of them.

`components/shared/data-table/*`, `components/shared/object-viewer/*`, `components/shared/access-denied.tsx`, and `components/toast/*` are consumed by feature/module code (or, for `access-denied.tsx`, by `lib/server/require-permission.tsx`) but import nothing from a feature/module themselves, so no cycle there either. `components/foundation/*` and `components/command/*` import only `@floating-ui/react`/`@tanstack/react-virtual`/`lucide-react`/`react`/each other — no feature/module dependency, no cycle. `components/command/*` (the `CommandPalette` component) is consumed by `components/shared/search-box.tsx`, the topbar search trigger; the sibling `CommandPaletteProvider` is not wired up anywhere. `components/shared/search-box.tsx` also imports `@/constants/nav-items` (the same plain-data assembly file `Sidebar` imports), `@/lib/shared/menu`, and `@/lib/shared/authorization` — all client-safe, no feature/module edge.

## Version Mismatches

Not applicable — `clients/admin` is still the only client app in the repo.

## Cross-Module Boundary Violations (backend only)

Not applicable — this is a client-app dependency graph, not backend.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
