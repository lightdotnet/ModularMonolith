---
name: razor-web
description: Playbook for adding or changing a screen in the server-rendered MVC + Razor Pages host (src/StarterKit.WebMvc) — service client for the backend module, controller or PageModel, views built from the shared tag helpers/components — using the razor-web-developer agent.
---

# Skill: Razor Web

## Purpose

Add or change a screen in `src/StarterKit.WebMvc` for a backend module while keeping the host's shape consistent: it talks to the backend only over HTTP through per-module service interfaces, and its UI is composed from the shared Bootstrap tag helpers, view components and vanilla JS modules.

## Inputs

- The backend module and the screen(s) wanted (list, create/edit, detail, action).
- Whether the screen is page-centric (Razor Page) or action/partial-centric (MVC controller + views) — default to a Razor Page for a CRUD screen, a controller for JSON/partial endpoints and multi-view flows.

## Workflow

1. **Plan first** (root CLAUDE.md §2.9) — list the endpoints used, the service interface methods to add, the pages/controllers, and the permissions; get approval.
2. **Confirm the contract** — read the module's REST controllers (`src/<Module>.Api/Controllers/`) and `<Module>.Contracts` DTOs; use [api-designer](../../agents/api-designer.md) if an endpoint is missing or its shape is unclear. A missing endpoint is a backend change — plan it separately with [dotnet-developer](../../agents/dotnet-developer.md).
3. **Service client** — add/extend `Services/<Module>/I<Resource>Client` (one method per endpoint, mirroring the controller) and its `HttpClient` implementation; register it with the module's named client.
4. **Screen** — delegate to [razor-web-developer](../../agents/razor-web-developer.md): controller or PageModel depending only on the interface, permission gate, views composed from the shared tag helpers (`<data-table>`, form fields, `confirm-button`, `page-header`, …).
5. **Review** — present the change to the user; on request run [security-reviewer](../../agents/security-reviewer.md) (auth, antiforgery, XSS), [code-reviewer](../../agents/code-reviewer.md), and [api-contract-reviewer](../../agents/api-contract-reviewer.md) (service client vs. REST contract drift).

## Expected Outputs

- A working screen on Desktop and Mobile, gated by permission, using the module's service client.
- A note of any new shared component added and why no existing one fit.

## Best Practices

- Service interfaces are the seam — keep them free of MVC types so an in-process implementation can replace the `HttpClient` one later.
- Prefer server-rendered partial HTML for data-table refreshes over JSON + client-side templating — columns are defined once in Razor.
- No jQuery; reuse `wwwroot/js` modules and the fetch helper (antiforgery header) rather than inline scripts.
