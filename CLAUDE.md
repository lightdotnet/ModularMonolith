# CLAUDE.md — Primary Source of Truth

This file is the entry point for every Claude Code session in this repository. Read it first, every session, before touching anything else. It holds only repository-wide guidance — project-specific detail lives with each project (see §3).

## 0. Language Convention

- The user gives instructions in Vietnamese. Understand and respond to Vietnamese requests normally — do not ask the user to switch to English.
- **Everything written into the repository — documentation and code comments — must be written in English**, regardless of what language the request came in. This keeps the codebase and its docs consistent for any future contributor.
- This applies to new content and edits alike: don't leave a Vietnamese comment or doc section next to English ones.

## 1. Repository Purpose

This repository is a **starter template monorepo for a full-stack application**: a C#/.NET backend and one or more frontend clients, meant to be cloned/forked as the starting point for new projects.

- **Backend** — `src/` — ASP.NET Core Web API, **C#**, organized as a **Modular Monolith**. One solution (`.sln`/`.slnx`), one deployable process. See [src/CLAUDE.md](src/CLAUDE.md) for module structure, stack, and backend-specific rules.
- **Clients** — `clients/` — one or more frontend apps, each in its own subfolder (e.g. `clients/admin/`, the current admin console). Each client consumes the backend exclusively over HTTP as a JSON API — the backend's MVC controllers are **API-only** (no server-rendered Razor views). See [clients/admin/CLAUDE.md](clients/admin/CLAUDE.md) for that app's stack and rules.
- **Integration** — the only contract between backend and any client is the HTTP API surface (routes, DTOs, status/error shapes). No side reaches into another's internals; there is no shared DB access or shared source between `src/` and `clients/*`. See [docs/integration.md](docs/integration.md) for the cross-cutting integration contract.

Consequences of this:

- There is one backend solution but potentially **multiple client apps** — don't assume `clients/` has only one subfolder. A request about "the frontend" without naming an app is ambiguous once more than one exists — ask.
- Module boundaries are the primary structural concern on the backend — each module's `<Module>.Contracts` project is the only allowed seam. See [src/CLAUDE.md](src/CLAUDE.md).
- Changes to the API contract ripple into every client that consumes that endpoint (and vice versa) — treat the HTTP contract as the seam to protect.
- This repo is a **template**: expect it to start mostly empty/skeletal and be filled in incrementally. Do not assume scaffolding exists until verified — check before describing structure.

## 2. AI Operating Rules

1. **Read only what the current task needs.** Do not open files, folders, or modules unrelated to the current request. Prefer `Glob`/`Grep` targeted lookups over broad tree walks.
2. **Prefer specialized agents over doing everything inline.** See [§4 Agent Usage](#4-agent-usage) — backend architecture, EF Core, API design, frontend architecture, security, performance, testing, docs, and dependency questions each have a dedicated agent. Delegate to them rather than reasoning about all domains yourself in the main context.
3. **Minimize token usage.** Summarize instead of pasting large file contents back to the user. Avoid re-reading files already read this session. Avoid speculative exploration "just in case."
4. **Repository analysis is incremental, never automatic.** Do not proactively scan or document the whole repo, the whole backend, or all of `clients/` unless the user explicitly asks. A request about one module or one client app is a request about that scope only.
5. **Documentation synchronization only happens on request.** Never regenerate or rewrite content under `src/docs/`, `clients/<app>/docs/`, `src/CLAUDE.md`, or `clients/<app>/CLAUDE.md` unless the user explicitly asks to generate or sync docs.
6. **Ask before assuming structure.** If it's unclear which module or which client app (once `clients/` has more than one) a request applies to, ask rather than guessing.
7. **No destructive or wide-blast-radius edits without confirmation.** Changes to the shared kernel/building blocks, cross-module refactors, changes to an API contract that one or more clients depend on, or dependency bumps touching both `src/` and `clients/*` require explicit user confirmation first.
8. **Language**: see [§0 Language Convention](#0-language-convention) — Vietnamese input is normal; everything written to the repo is English.
9. **Code-change workflow gate.** For any request to add code, modify existing code, or add a feature: always produce a plan first and present it for the user's review — do not write any code until the user explicitly approves the plan. Once approved, implement it, delegating to the relevant agents/skills/workflows (§4–§6) wherever they apply. When implementation is complete, present the changed code back to the user for review before doing anything further — don't chain straight into testing or docs. **Running the automated test suite and updating documentation each require a separate, explicit follow-up instruction** from the user; never trigger either automatically right after implementing, even if the approved plan mentioned adding tests or docs. See [implement-feature](.claude/workflows/implement-feature.md) and [create-feature](.claude/skills/create-feature.md).

## 3. Where Things Live

- **`.claude/`** — Claude development infrastructure only: reusable agents, skills, workflows, commands, doc-generation templates, and this project's own working-rules/meta-maintenance docs (`AI_CONTEXT.md`, `ROT.md`, `WORKFLOWS.md`). Nothing project-specific belongs here — see [.claude/AI_CONTEXT.md](.claude/AI_CONTEXT.md) for detailed working rules.
- **`src/CLAUDE.md` + `src/docs/`** — everything specific to the backend solution: module/architecture detail, conventions, known debt. See [src/CLAUDE.md](src/CLAUDE.md).
- **`clients/<app-name>/CLAUDE.md` + `clients/<app-name>/docs/`** — everything specific to that client app. See [clients/admin/CLAUDE.md](clients/admin/CLAUDE.md).
- **`docs/`** (repo root) — genuinely cross-cutting knowledge that spans both backend and clients (the integration boundary itself), not owned by either project. See [docs/integration.md](docs/integration.md).
- Manually-authored documentation must be preserved during any sync — never silently overwritten. See [sync-documentation workflow](.claude/workflows/sync-documentation.md).
- The `.claude/` configuration itself (agents/skills/workflows/templates) is reviewed on its own recurring schedule for redundant/outdated/trivial content — see [.claude/ROT.md](.claude/ROT.md).

## 4. Agent Usage

Specialized agents live in [.claude/agents/](.claude/agents/). Prefer delegating to them over reasoning inline:

| Agent | Use for |
|---|---|
| [architecture-reviewer](.claude/agents/architecture-reviewer.md) | Backend layering, module boundaries, dependency direction within/between modules under `src/` |
| [dotnet-architect](.claude/agents/dotnet-architect.md) | Backend project/module structure, strategic DDD (bounded contexts, context mapping, cross-module integration mechanism), framework choices, new module shape |
| [ddd-modeler](.claude/agents/ddd-modeler.md) | Tactical DDD inside a module's domain layer — aggregate boundaries/invariants, entities vs. value objects, domain events, anemic-model detection |
| [efcore-specialist](.claude/agents/efcore-specialist.md) | EF Core models, migrations, query performance, DbContext design per module |
| [api-designer](.claude/agents/api-designer.md) | Backend REST API contract design, versioning, DTO shape |
| [dotnet-developer](.claude/agents/dotnet-developer.md) | Implementing an approved backend change under `src/` — writes C# to convention, build-sanity only (never runs the test suite) |
| [code-reviewer](.claude/agents/code-reviewer.md) | General C#/.NET backend code quality review |
| [nextjs-architect](.claude/agents/nextjs-architect.md) | Next.js/React/TypeScript structure for a given client app — routing, data fetching, state management, component architecture |
| [nextjs-developer](.claude/agents/nextjs-developer.md) | Implementing an approved change under `clients/admin/` — writes Next.js 16/React 19/TS to convention, lint/build-sanity only |
| [frontend-code-reviewer](.claude/agents/frontend-code-reviewer.md) | React/TypeScript code quality review for a given client app |
| [api-contract-reviewer](.claude/agents/api-contract-reviewer.md) | Consistency between backend API contracts and how a given client consumes them; drift detection |
| [security-reviewer](.claude/agents/security-reviewer.md) | Vulnerabilities, secrets, auth/authz gaps — backend and any client app |
| [performance-reviewer](.claude/agents/performance-reviewer.md) | Hot paths, allocations, async misuse, backend/client performance |
| [testing-reviewer](.claude/agents/testing-reviewer.md) | Test coverage/quality — backend (xUnit-style) and client apps (component/unit) |
| [documentation-writer](.claude/agents/documentation-writer.md) | Generating/updating docs from code |
| [dependency-analyzer](.claude/agents/dependency-analyzer.md) | Backend project/package references and each client's npm dependencies, coupling |

Rule of thumb: if a task maps cleanly to one row above, delegate to that agent instead of doing the analysis in the main thread.

## 5. Skill Usage

Reusable playbooks live in [.claude/skills/](.claude/skills/) — see each file for purpose, inputs, workflow, and best practices:

- [create-feature](.claude/skills/create-feature.md) — full-stack feature (backend module change + client UI + contract), [refactor](.claude/skills/refactor.md)
- [ddd-modeling](.claude/skills/ddd-modeling.md) — tactical domain model for a backend module, [clean-architecture-split](.claude/skills/clean-architecture-split.md) — the (as-yet-unused) single-project → 4-project module split
- [review-code](.claude/skills/review-code.md), [review-architecture](.claude/skills/review-architecture.md)
- [generate-docs](.claude/skills/generate-docs.md), [sync-docs](.claude/skills/sync-docs.md)
- [analyze-solution](.claude/skills/analyze-solution.md) (backend), [analyze-module](.claude/skills/analyze-module.md), [analyze-project](.claude/skills/analyze-project.md)
- [analyze-frontend](.claude/skills/analyze-frontend.md) (all of `clients/`), [analyze-client](.claude/skills/analyze-client.md) (one client app)
- [efcore](.claude/skills/efcore.md), [api](.claude/skills/api.md), [nextjs](.claude/skills/nextjs.md), [testing](.claude/skills/testing.md), [performance](.claude/skills/performance.md)

## 6. Workflow Usage

Session- and task-level workflows live in [.claude/workflows/](.claude/workflows/) — see [.claude/WORKFLOWS.md](.claude/WORKFLOWS.md) for the index:

- [new-session](.claude/workflows/new-session.md) — start of every session
- [scaffold-project](.claude/workflows/scaffold-project.md) — initial backend/client scaffolding for this template
- [analyze-folder](.claude/workflows/analyze-folder.md), [analyze-solution](.claude/workflows/analyze-solution.md)
- [implement-feature](.claude/workflows/implement-feature.md)
- [review-repository](.claude/workflows/review-repository.md)
- [sync-documentation](.claude/workflows/sync-documentation.md)
- [end-session](.claude/workflows/end-session.md) — end of every non-trivial session

## 7. Context Management

- Load context in this order: this file → the specific project's `CLAUDE.md`/`docs/` relevant to the task → the specific code files needed. Never load all of a project's `docs/` at once.
- Prefer `Grep`/`Glob` to locate the relevant module/project/client app before reading files.
- When a task is scoped to one module or one client app, do not read sibling modules/apps "for context" unless a real dependency exists (verified via project references or actual API calls, not assumption).
- Summarize large findings; don't paste entire files into the conversation when a targeted excerpt will do.
- See [.claude/AI_CONTEXT.md](.claude/AI_CONTEXT.md) for detailed working rules.

## 8. Documentation Synchronization Rules

- Sync is **explicit and pull-based**: it happens only when the user runs a sync/generate request (see [sync-documentation](.claude/workflows/sync-documentation.md), [sync-docs skill](.claude/skills/sync-docs.md)).
- Sync must diff current docs against current code, update what changed, remove what's stale, and leave manually-authored content untouched.
- Never sync as a side effect of an unrelated task (e.g., don't "helpfully" update docs while implementing a feature unless asked).

## 9. Quick Reference for Common Commands

| User says | Do this |
|---|---|
| "Scaffold the project" / "set up the initial structure" | [.claude/workflows/scaffold-project.md](.claude/workflows/scaffold-project.md) |
| "Analyze the backend/solution" | [.claude/workflows/analyze-solution.md](.claude/workflows/analyze-solution.md) |
| "Analyze this module" | [.claude/skills/analyze-module.md](.claude/skills/analyze-module.md) |
| "Analyze this project" | [.claude/skills/analyze-project.md](.claude/skills/analyze-project.md) |
| "Analyze all the clients/frontends" | [.claude/skills/analyze-frontend.md](.claude/skills/analyze-frontend.md) |
| "Analyze the admin client / this client app" | [.claude/skills/analyze-client.md](.claude/skills/analyze-client.md) |
| "Analyze this folder" | [.claude/workflows/analyze-folder.md](.claude/workflows/analyze-folder.md) |
| "Generate documentation" | [.claude/skills/generate-docs.md](.claude/skills/generate-docs.md) |
| "Sync documentation" | [.claude/workflows/sync-documentation.md](.claude/workflows/sync-documentation.md) |
| "Review architecture" | [.claude/skills/review-architecture.md](.claude/skills/review-architecture.md) |
| "Review code" | [.claude/skills/review-code.md](.claude/skills/review-code.md) |
| "Design a domain model / aggregate" / "where should this rule live" | [.claude/skills/ddd-modeling.md](.claude/skills/ddd-modeling.md) |
| "Split a module into Clean Architecture layers" | [.claude/skills/clean-architecture-split.md](.claude/skills/clean-architecture-split.md) |
| "Implement a feature" | [.claude/workflows/implement-feature.md](.claude/workflows/implement-feature.md) |
| "Update CLAUDE documentation" | [.claude/skills/sync-docs.md](.claude/skills/sync-docs.md), scoped to `.claude/` docs only |
| "Run a ROT review of .claude" / "check for stale agents/skills" | [.claude/ROT.md](.claude/ROT.md) |
| "Load backend/frontend/full context" | `/context-backend`, `/context-frontend`, or `/context-full` ([.claude/commands/](.claude/commands/)) |

---
_Last synced: 2026-09-07_
