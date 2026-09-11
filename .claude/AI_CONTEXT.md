# AI Working Rules

Detailed operating constraints for Claude when working in this repository. `CLAUDE.md` is the authoritative summary; this file expands on the reasoning and edge cases.

## Language Convention

See `CLAUDE.md` §0. The user writes requests in Vietnamese — treat that as completely normal, respond and reason about the request in whatever language fits the conversation. The constraint is one-directional: it applies to what gets **written into the repository**, not to the conversation. Concretely:

- Markdown docs (`.claude/**`, generated docs, any README-style file) — English.
- Code comments, commit-adjacent text, docstrings — English.
- A Vietnamese instruction does not get "translated" into a Vietnamese doc section — the output is English regardless of the input language.

## Repository Shape Assumptions

Always assume, until verified otherwise for the specific task at hand:

- **One backend solution** under `src/` (ASP.NET Core, C#), organized as a **Modular Monolith** — module structure convention, current modules, and backend-specific rules live in [../src/CLAUDE.md](../src/CLAUDE.md), not here.
- **One or more client apps** under `clients/<app-name>/` — do not assume there's only one. Check `Glob clients/*/` before describing "the frontend" as if it were singular; each app's own rules live in `clients/<app-name>/CLAUDE.md` (e.g. [../clients/admin/CLAUDE.md](../clients/admin/CLAUDE.md)).
- **The backend and each client only talk over HTTP.** No shared source, no shared DB access, no in-process calls between `src/` and `clients/*`. See [../docs/integration.md](../docs/integration.md) for the cross-cutting integration contract.
- **This is a template repository.** It may be nearly empty (no code yet) or partially scaffolded. Don't describe structure that hasn't been verified to exist — check with `Glob`/`Grep` first, and say "not yet scaffolded" rather than guessing.

## Context-Loading Strategy

1. **Task scoping first.** Before reading code, identify the minimum module/project/client app the task actually touches. If ambiguous — including "which client app" once `clients/` has more than one — ask the user rather than reading broadly to disambiguate yourself.
2. **Index before content.** Use `Glob`/`Grep` to locate relevant `.csproj`/`.sln`/namespaces (backend) or `package.json`/route folders (a client app) before opening files. Don't open files "to see what's there."
3. **Read incrementally.** Open only the files needed for the current step of the task. Re-scope and read more only when a genuine dependency is found (e.g., a referenced project, a shared building block, an API route a client calls).
4. **No repo-wide scans without an explicit request.** "Analyze this folder" means that folder. "Analyze this module" means that module and its direct dependencies — not sibling modules. "Analyze the frontend" without a named app means all of `clients/` (see [skills/analyze-frontend.md](skills/analyze-frontend.md)); a named app means just that one (see [skills/analyze-client.md](skills/analyze-client.md)).
5. **Cache findings in the right place.** Verified facts about structure go into the scoped project doc — `src/CLAUDE.md`/`src/docs/` for backend, `clients/<app-name>/CLAUDE.md`/`clients/<app-name>/docs/` for that client app, or the root `docs/` for genuinely cross-cutting facts — only when a sync/generate step is explicitly requested. Not into ad hoc notes that vanish at session end.

## Token Efficiency Rules

- Prefer `Grep -n` targeted excerpts over full-file reads when only a symbol or pattern is needed.
- Prefer delegating multi-file investigations to a specialized agent (see `CLAUDE.md` §4) so exploration detail stays out of the main context — the agent reports a summary back.
- Never paste an entire large generated file (migrations, designer files, `obj/`/`bin/`/`node_modules/`/`.next/` artifacts) into the conversation.
- Summarize diffs and findings in prose/tables rather than reproducing full file contents when reporting back to the user.
- Avoid redundant re-reads: if a file was already read this session and hasn't been edited since, don't re-read it "to be sure."

## When to Delegate vs. Do Inline

Delegate to a specialized agent when:

- The task maps directly to one agent's domain (backend architecture, strategic/tactical DDD, EF Core, API design, frontend architecture, security, performance, testing, docs, dependencies).
- Implementing an approved plan of any real size — hand the code changes to `dotnet-developer` (backend) / `nextjs-developer` (client app) rather than editing inline.
- The investigation would require reading many files whose contents don't need to stay in the main context.
- A second, independent opinion is valuable (e.g. security or architecture review).

Do inline when:

- The task is a small, well-scoped edit in a file already open/known.
- The user is mid-conversation about a specific line/function and wants a direct answer.

## Code-Change Workflow Gate

See `CLAUDE.md` §2.9. This is the standard lifecycle for any request that adds or modifies code (a feature, a fix, a refactor) — not just the big ones:

1. **Plan → present → wait.** Write the plan, show it to the user, and stop. Do not open an editor on production code before the user has explicitly said to proceed. This holds even for changes that feel small — the user asked for this gate specifically so they control the pacing, not so Claude can judge "this one's obviously fine."
2. **Implement using the normal toolbox.** Once approved, delegate to the relevant agents/skills/workflows exactly as `CLAUDE.md` §2.2/§4–§6 already describe — the code changes themselves go to `dotnet-developer` (backend) / `nextjs-developer` (client app). This gate doesn't change *how* work gets delegated, only *when* it's allowed to start.
3. **Present the result → wait again.** When the code change is done, stop and hand it back for review before doing anything else. Don't chain into running tests or updating docs as a "helpful" next step.
4. **Tests and docs are separate, explicit asks.** Running the automated test suite (`dotnet test`, `npm test`/`vitest`/`playwright`, etc.) and updating documentation each need their own follow-up instruction from the user after they've reviewed the code. This holds even if the approved plan mentioned "add tests" or "update docs" as part of the work — writing test *code* can be part of implementation if the plan called for it, but *executing* the suite is a separate, explicit step.
5. **Basic build sanity is not "running tests."** Confirming the code compiles (or a scaffold's dev server starts) as you go is fine and expected — it's not the same as running the project's test suite, which stays gated.

## Documentation Discipline

- Never regenerate or rewrite `src/docs/**`, `clients/<app-name>/docs/**`, `src/CLAUDE.md`, or `clients/<app-name>/CLAUDE.md` as a side effect of an unrelated task.
- When a sync is requested, follow [workflows/sync-documentation.md](workflows/sync-documentation.md) exactly: diff against code, update changed sections, remove stale ones, preserve manually-authored content.
- Templates in `docs/templates/` are structural skeletons — copy their structure into `src/docs/`/`clients/<app-name>/docs/` outputs, don't edit the templates themselves during normal doc generation.
- Test-suite execution follows the same "only on explicit request" discipline as docs — see Code-Change Workflow Gate above.

## Full-Stack Safety Rules

- Before changing an API route/DTO shape, check every client app under `clients/*` for actual usages (fetch calls, typed client, route handlers) — don't assume only one client is affected, and don't assume any client is unaffected without checking.
- Before changing a module's public contract (its `Api`/exposed `Application` interfaces), check for other backend modules or the composition-root host that depend on it.
- Cross-module or cross-stack (backend+client in the same change) refactors require explicit user confirmation before starting (see `CLAUDE.md` §2.7).
- Treat the shared/building-blocks backend project as higher-risk to change — every module may depend on it.
