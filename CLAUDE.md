# CLAUDE.md — Primary Source of Truth

This file is the entry point for every Claude Code session in this repository. Read it first, every session, before touching anything else.

## 0. Language Convention

- The user gives instructions in Vietnamese. Understand and respond to Vietnamese requests normally — do not ask the user to switch to English.
- **Everything written into the repository — documentation and code comments — must be written in English**, regardless of the language of the request.
- This applies to new content and edits alike: don't leave a Vietnamese comment or doc section next to English ones.

## 1. Repository Purpose

This branch (`dev/core`) holds the **core of the StarterKit Modular Monolith template**: the reusable C#/.NET framework building blocks, one composition-root host, one reference business module (Identity), and the per-provider migrator apps, plus their tests. It contains no other business modules and no client apps — don't assume any exist; verify before describing structure.

- **One solution** — `StarterKit.slnx` at the repo root, targeting .NET 10 (`net10.0`), with solution folders `/src/_framework/`, `/src/_host/`, `/src/identity-module/`, `/src/_migrations/`, and `/tests/`.
- **Flat layout** — every project sits directly under `src/` (or `tests/`) with a short folder name (the migrators under `src/Migrations/`); assembly/root namespaces carry the full name: `StarterKit.<Project>` for framework projects and the host, `StarterKit.Modules.<Module>[.Contracts|.Web]` for module projects. The migrator assemblies keep their folder names.

| Project | Assembly | Responsibility |
|---|---|---|
| `src/Shared` | `StarterKit.Shared` | Shared kernel: DDD building blocks (entity bases, `DomainEvent`, value objects), the `IntegrationEvent` base, current-user/date-time abstractions, authorization policies, mediator pipeline behaviours, paging/search queries, shared constants — see [Shared](docs/architecture/Shared.md) |
| `src/Infrastructure` | `StarterKit.Infrastructure` | ASP.NET Core hosting concerns: DI wiring, controller bases and endpoint attributes, module registration (`AppModule`/`AppModuleEndpoint`), caching, CORS, health checks, mapping, logging — see [Infrastructure](docs/architecture/Infrastructure.md) |
| `src/Persistence` | `StarterKit.Persistence` | EF Core: `BaseDbContext`, audit and domain-event dispatch on save, entity/index builder extensions, cache and dynamic-table repositories, multi-provider support (InMemory, PostgreSQL, MSSQL, Sqlite), migration support — see [Persistence](docs/architecture/Persistence.md) |
| `src/EventBusMassTransitRabbitMQ` | `StarterKit.EventBusMassTransitRabbitMQ` | Integration-event bus: `IEventBus` registration over MassTransit/RabbitMQ from configuration (no-op bus when disabled), consumer and consumer-definition bases, module consumer registration — see [EventBusMassTransitRabbitMQ](docs/architecture/EventBusMassTransitRabbitMQ.md) |
| `src/Host` | `StarterKit.Host` | Composition root and the only deployable: co-hosts the JSON API and the Identity Razor Pages — see [Host](docs/architecture/Host.md) |
| `src/Identity` | `StarterKit.Modules.Identity` | Identity module implementation: ASP.NET Core Identity store, self-issued JWT/refresh/session/hub tokens, external login, Active Directory, user/role endpoints — see [Identity](docs/architecture/Identity.md) |
| `src/Identity.Contracts` | `StarterKit.Modules.Identity.Contracts` | Identity's cross-module seam: `IIdentityModuleApi`, `UserSummary`, integration events |
| `src/Identity.Web` | `StarterKit.Modules.Identity.Web` | Identity's Razor Pages: login and the Microsoft external-login relay; co-hosted by `Host` or run standalone |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | `MSSQL`, `PostgreSQL`, `Sqlite` | Per-provider EF Core migrations and migrate-and-seed console apps — see [migrations.md](docs/conventions/migrations.md) |

- **Tests** — `tests/Framework.Tests` (framework projects) and `tests/Identity.Tests` (Identity module), both configured by `tests/ModuleTests.props`; layout and conventions are in [coding-conventions.md § Testing Conventions](docs/conventions/coding-conventions.md#testing-conventions).
- **Project references** — the one canonical diagram is [dependency-graph.md](docs/architecture/dependency-graph.md); layering and runtime flows are in [architecture.md](docs/architecture/architecture.md).

Consequences:

- Every public type/member in a framework project is a contract for every module built on it — a change to it is potentially breaking. The same holds for a module's `.Contracts` project toward the modules that consume it.
- Dependency direction is fixed:
  - Framework: `Infrastructure → Shared`, `Persistence → Shared`, `EventBusMassTransitRabbitMQ → Shared`; `Shared` references no solution project; no framework project references a module, the host, or a migrator.
  - Modules: a `<Module>.Contracts` project references only `Shared`; a module's implementation references its own `.Contracts` plus framework projects, and other modules only through their `.Contracts`; a module's `.Web` project references its own module plus framework projects. No module references `Host` or a migrator.
  - Outside a module's own projects and its test project, only the composition roots reference a module's implementation — `Host` and the migrators under `src/Migrations/` — and only `Host` references a module's `.Web` project.
- Modules talk to each other only through a `<Module>.Contracts` seam, a domain event, an integration event, or a denormalized snapshot — the framework must not force anything else.
- Keep the framework small: a building block belongs in a framework project only if it is genuinely reused across modules; anything specific to one module stays in that module.

## 2. AI Operating Rules

1. **Read only what the current task needs.** Prefer `Glob`/`Grep` targeted lookups over broad tree walks.
2. **Prefer specialized agents over doing everything inline.** See [§4](#4-agent-usage).
3. **Minimize token usage.** Summarize instead of pasting large file contents. Avoid re-reading files already read this session and speculative exploration.
4. **Repository analysis is incremental, never automatic.** A request about one project or folder is a request about that scope only.
5. **Documentation synchronization only happens on request.** Never regenerate or rewrite this file, `README.md`, `docs/`, or `.claude/` docs unless the user explicitly asks to generate or sync docs.
6. **Ask before assuming structure.** If it's unclear which project a request applies to, ask.
7. **No destructive or wide-blast-radius edits without confirmation.** Public-API changes in a framework project (especially `Shared`) or a module's `.Contracts`, changes to central build files (`Directory.Build.props`, `Directory.Packages.props`, `tests/ModuleTests.props`), and dependency bumps require explicit user confirmation first.
8. **Language**: see [§0](#0-language-convention).
9. **Code-change workflow gate.** For any request to add code, modify existing code, or add a feature: always produce a plan first and present it for review — do not write any code until the user explicitly approves the plan. Once approved, implement it, delegating to the relevant agents/skills/workflows (§4–§6). When implementation is complete, present the changed code back for review before doing anything further. **Running the test suite and updating documentation each require a separate, explicit follow-up instruction** from the user; never trigger either automatically after implementing, even if the approved plan mentioned tests or docs. See [implement-feature](.claude/workflows/implement-feature.md).

## 3. Where Things Live

- **`src/`** — the framework, host, module, and migrator projects (§1). **`tests/`** — their test projects and shared test props.
- **Root build files** — `StarterKit.slnx`, `Directory.Build.props` (target framework, nullable, implicit usings), `Directory.Packages.props` (central package versions — the version of record).
- **`docs/`** — where generated documentation for the solution goes (`docs/architecture/`, `docs/conventions/`), created only through [generate-docs](.claude/skills/generate-docs/SKILL.md) on request. How to build, run, and test is in [development-guide.md](docs/conventions/development-guide.md).
- **`.claude/`** — Claude development infrastructure only: agents, skills, workflows, doc templates, hooks/settings, and working-rules/meta-maintenance docs ([AI_CONTEXT.md](.claude/AI_CONTEXT.md), [ROT.md](.claude/ROT.md), [WORKFLOWS.md](.claude/WORKFLOWS.md)).
- Manually authored documentation must be preserved during any sync — see [sync-documentation](.claude/workflows/sync-documentation.md).

## 4. Agent Usage

Specialized agents live in [.claude/agents/](.claude/agents/). Prefer delegating to them:

| Agent | Use for |
|---|---|
| [dotnet-architect](.claude/agents/dotnet-architect.md) | Where a building block belongs (which framework project, or not in the framework), module extension points, library choices |
| [ddd-modeler](.claude/agents/ddd-modeler.md) | DDD building blocks in `Shared` and domain models built on them — aggregates, invariants, value objects, domain events |
| [efcore-specialist](.claude/agents/efcore-specialist.md) | `Persistence` and module contexts — base context, audit/domain-event dispatch, repositories, providers, migrations, query performance |
| [api-designer](.claude/agents/api-designer.md) | Controller bases, endpoint registration, response envelope, versioning, auth attributes |
| [dotnet-developer](.claude/agents/dotnet-developer.md) | Implementing an approved change under `src/`/`tests/` — build-sanity only, never runs the test suite |
| [architecture-reviewer](.claude/agents/architecture-reviewer.md) | Layering, dependency direction, shared-kernel cohesion, module boundaries |
| [code-reviewer](.claude/agents/code-reviewer.md) | General C# code quality review |
| [security-reviewer](.claude/agents/security-reviewer.md) | Vulnerabilities, secrets, auth/authz building blocks, token issuance, unsafe defaults |
| [performance-reviewer](.claude/agents/performance-reviewer.md) | Hot paths, allocations, async misuse, per-request framework overhead |
| [testing-reviewer](.claude/agents/testing-reviewer.md) | Test coverage/quality of `tests/Framework.Tests` and `tests/Identity.Tests` |
| [dependency-analyzer](.claude/agents/dependency-analyzer.md) | Project/package references, central versions, circular or direction-violating references |
| [documentation-writer](.claude/agents/documentation-writer.md) | Generating/updating docs from code, on explicit request only |

## 5. Skill Usage

Skills live in [.claude/skills/](.claude/skills/) as `<name>/SKILL.md` — each is invocable as `/<name>`:

- [analyze-solution](.claude/skills/analyze-solution/SKILL.md), [analyze-project](.claude/skills/analyze-project/SKILL.md)
- [review-code](.claude/skills/review-code/SKILL.md), [review-architecture](.claude/skills/review-architecture/SKILL.md)
- [refactor](.claude/skills/refactor/SKILL.md), [ddd-modeling](.claude/skills/ddd-modeling/SKILL.md), [efcore](.claude/skills/efcore/SKILL.md), [api](.claude/skills/api/SKILL.md), [testing](.claude/skills/testing/SKILL.md), [performance](.claude/skills/performance/SKILL.md)
- [generate-docs](.claude/skills/generate-docs/SKILL.md), [sync-docs](.claude/skills/sync-docs/SKILL.md)

## 6. Workflow Usage

Workflows live in [.claude/workflows/](.claude/workflows/) — see [.claude/WORKFLOWS.md](.claude/WORKFLOWS.md) for the index: [new-session](.claude/workflows/new-session.md), [analyze-folder](.claude/workflows/analyze-folder.md), [implement-feature](.claude/workflows/implement-feature.md), [review-repository](.claude/workflows/review-repository.md), [sync-documentation](.claude/workflows/sync-documentation.md), [end-session](.claude/workflows/end-session.md).

## 7. Framework Conventions

The short-form rules; the detail behind them is in [coding-conventions.md](docs/conventions/coding-conventions.md).

- **Packages**: versions are set centrally in `Directory.Packages.props`; don't put `Version=` on a `PackageReference` in a project (the test projects via `tests/ModuleTests.props` and the migrators opt out — see [dependency-graph.md § Version Mismatches](docs/architecture/dependency-graph.md#version-mismatches)). Many building blocks derive from vendor `Lightsoft.*` packages (namespaces `Light.*`) — check the vendor base type before re-implementing behavior.
- **Errors**: expected failures return `Result`/`Result<T>`, not exceptions. Input/format validation (required, length, range) is FluentValidation; domain types enforce domain rules only.
- **API responses**: controllers derive from the `Infrastructure/Endpoints` bases, and responses returned through the base `Ok<T>()` are auto-wrapped in the response envelope — never hand-wrap.
- **DDD**: behavior and invariants live on the entity/aggregate, not in handlers; domain events derive from `Shared`'s `DomainEvent` and are dispatched through `Persistence`'s dispatch on save.
- **Domain vs. integration events**: a domain event stays in-process (dispatched through the mediator). A message that must cross a module or service boundary asynchronously is an integration event — a record deriving from `Shared`'s `IntegrationEvent`, published through `IEventBus` and consumed via the `EventBusMassTransitRabbitMQ` bases. See [EventBusMassTransitRabbitMQ](docs/architecture/EventBusMassTransitRabbitMQ.md).
- **DI**: each area exposes a `static class DependencyInjection` with `Add<Feature>` (and `Use<Feature>` for middleware) extension methods.
- **Formatting**: one parameter per line for records/constructors, base type on its own line, multi-argument calls broken out.
- **EF Core**: inside an `entity.ToTable(...)` block, put `HasIndex` calls right after `ToTable`; changes to persistence behavior must hold for every supported provider.
- **Tests**: xunit.v3 + Moq (via `tests/ModuleTests.props`), unit tests with mocked dependencies; a test project's folders mirror the source project's folders.

## 8. Documentation Synchronization Rules

- Sync is **explicit and pull-based**: it happens only when the user asks (see [sync-documentation](.claude/workflows/sync-documentation.md), [sync-docs](.claude/skills/sync-docs/SKILL.md)).
- Sync must diff current docs against current code, update what changed, remove what's stale, and leave manually authored content untouched.
- Never sync as a side effect of an unrelated task.

## 9. Quick Reference

| User says | Do this |
|---|---|
| "Analyze the solution" | [analyze-solution](.claude/skills/analyze-solution/SKILL.md) |
| "Analyze this project" | [analyze-project](.claude/skills/analyze-project/SKILL.md) |
| "Analyze this folder" | [analyze-folder](.claude/workflows/analyze-folder.md) |
| "Implement / change X" | [implement-feature](.claude/workflows/implement-feature.md) |
| "Review code" | [review-code](.claude/skills/review-code/SKILL.md) |
| "Review architecture" | [review-architecture](.claude/skills/review-architecture/SKILL.md) |
| "Review the repository" | [review-repository](.claude/workflows/review-repository.md) |
| "Design a building block / where should this rule live" | [ddd-modeling](.claude/skills/ddd-modeling/SKILL.md) |
| "Generate documentation" | [generate-docs](.claude/skills/generate-docs/SKILL.md) |
| "Sync documentation" | [sync-documentation](.claude/workflows/sync-documentation.md) |
| "Run a ROT review of .claude" | [.claude/ROT.md](.claude/ROT.md) |

---
_Last synced: 2026-09-30_
