# AI Working Rules

Detailed operating constraints for Claude when working in this repository. Root `CLAUDE.md` is the authoritative summary; this file expands on the reasoning and edge cases.

## Language Convention

See `CLAUDE.md` §0. The user writes requests in Vietnamese — treat that as completely normal. The constraint is one-directional: it applies to what gets **written into the repository**, not to the conversation:

- Markdown docs (`.claude/**`, generated docs, any README-style file) — English.
- Code comments, commit-adjacent text, docstrings — English.
- A Vietnamese instruction does not get "translated" into a Vietnamese doc section — the output is English regardless of the input language.

## Repository Shape Assumptions

- **One .NET solution** (`StarterKit.slnx` at the repo root) holding the framework projects, the `StarterKit.WebApi` composition root with its Aspire app host and service defaults (`StarterKit.AppHost`, `StarterKit.ServiceDefaults`), and the Identity and Notifications modules under `src/`, and their tests under `tests/` — see `CLAUDE.md` §1 and §3.
- **One client app**, `clients/admin/` (Next.js admin console consuming Identity and Notifications). Its own rules live in [../clients/admin/CLAUDE.md](../clients/admin/CLAUDE.md) and its docs under `clients/admin/docs/`. `Glob clients/*/` before describing "the frontend" as if it were singular.
- **The backend and a client talk only over HTTP** (JSON API + SignalR) — no shared source, DB access, or in-process calls between `src/` and `clients/*`. The cross-cutting contract is in [../docs/integration.md](../docs/integration.md).
- **Identity and Notifications are the only business modules.** Don't describe or assume any other module, host, or client exists — verify with `Glob`/`Grep` first. Code comments or configuration may mention modules that live elsewhere; that is not evidence they exist here.

## Context-Loading Strategy

1. **Task scoping first.** Before reading code, identify the minimum project/folder — or client app area — the task actually touches. If ambiguous, ask rather than reading broadly.
2. **Index before content.** Use `Glob`/`Grep` to locate relevant `.csproj`/namespaces (backend) or route/feature folders (client app) before opening files. Don't open files "to see what's there."
3. **Read incrementally.** Open only the files needed for the current step. Expand only when a genuine dependency is found (a referenced project, a vendor base type, an API route the client calls).
4. **No repo-wide scans without an explicit request.** "Analyze this folder" means that folder; "analyze this project" means that project and its direct references; "analyze the client" means `clients/admin/`, not the backend.
5. **Cache findings in the right place.** Verified structural facts go into generated docs — `docs/` for the backend, `clients/admin/docs/` for the client app, `docs/integration.md` for the boundary — only when a sync/generate step is explicitly requested. Not into ad hoc notes that vanish at session end.

## Token Efficiency Rules

- Prefer `Grep -n` targeted excerpts over full-file reads when only a symbol or pattern is needed.
- Prefer delegating multi-file investigations to a specialized agent (see `CLAUDE.md` §4) so exploration detail stays out of the main context.
- Never paste large generated files (`obj/`/`bin/`/`node_modules/`/`.next/` artifacts, migration designer files, lockfiles) into the conversation.
- Summarize diffs and findings in prose/tables rather than reproducing full file contents.
- Don't re-read a file already read this session that hasn't been edited since.

## When to Delegate vs. Do Inline

Delegate to a specialized agent when:

- The task maps directly to one agent's domain (architecture, DDD, EF Core, API conventions, frontend architecture, backend ↔ client contract, security, performance, testing, docs, dependencies).
- Implementing an approved plan of any real size — hand the code changes to `dotnet-developer` (backend) or `nextjs-developer` (client app) rather than editing inline.
- The investigation would require reading many files whose contents don't need to stay in the main context.
- A second, independent opinion is valuable (e.g. security or architecture review).

Do inline when:

- The task is a small, well-scoped edit in a file already open/known.
- The user is mid-conversation about a specific line/function and wants a direct answer.

## Code-Change Workflow Gate

See `CLAUDE.md` §2.9. This is the standard lifecycle for any request that adds or modifies code — backend or client app, not just the big ones:

1. **Plan → present → wait.** Write the plan, show it to the user, and stop. Do not edit production code before the user has explicitly said to proceed, even for changes that feel small.
2. **Implement using the normal toolbox.** Once approved, delegate as `CLAUDE.md` §4–§6 describe — the code changes themselves go to `dotnet-developer` (backend) or `nextjs-developer` (client app).
3. **Present the result → wait again.** When the change is done, hand it back for review. Don't chain into running tests or updating docs.
4. **Tests and docs are separate, explicit asks.** Running the test suite and updating documentation each need their own follow-up instruction, even if the approved plan mentioned them — writing test *code* can be part of implementation if the plan called for it; *executing* the suite is a separate step. How the backend suite is run is in [development-guide.md § Running Tests](../docs/conventions/development-guide.md#running-tests); the client app has no test suite yet.
5. **Basic build sanity is not "running tests."** Confirming the code compiles (`dotnet build`, or `pnpm lint`/`pnpm build` in the client app) as you go is expected.

## Documentation Discipline

- Never regenerate or rewrite root `CLAUDE.md`, `README.md`, `docs/**`, `clients/admin/CLAUDE.md`, `clients/admin/README.md`, or `clients/admin/docs/**` as a side effect of an unrelated task.
- When a sync is requested, follow [workflows/sync-documentation.md](workflows/sync-documentation.md).
- Templates in `.claude/docs/templates/` are structural skeletons — copy their structure into outputs; don't edit the templates during normal doc generation.

## Automatic Context Recap (Hooks)

Unlike everything else in `.claude/` (pull-based — invoked on request), one `SessionStart` hook registered in [`settings.json`](settings.json) runs automatically, with matchers `resume` and `compact` — the two points a session's context is most likely to drift: a resumed session (repo state may have changed) and a conversation compaction (auto or manual). It runs [`hooks/context-recap.js`](hooks/context-recap.js), which injects the actual `git status --short -b` output plus a condensed reminder of the operating rules most likely to get lost in a summary (the code-change workflow gate, scoped reading, delegation defaults).

A cold `startup` doesn't need this: root `CLAUDE.md` loads fresh in that case.

## Framework Safety Rules

Public framework API, a module's `.Contracts`, and the central build files are contracts — see root `CLAUDE.md` §1 Consequences and §2.7 (confirm before changing them).

## Full-Stack Safety Rules

- Before changing an API route or a request/response shape, check every client app under `clients/*` for actual call sites (via [api-contract-reviewer](agents/api-contract-reviewer.md)) — don't assume a client is unaffected without checking.
- A change spanning backend and client in one go settles the contract first — see [skills/create-feature](skills/create-feature/SKILL.md).

---
_Last synced: 2026-09-30_
