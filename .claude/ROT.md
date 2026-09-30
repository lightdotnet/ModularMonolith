# ROT.md — Scheduled Maintenance for `.claude/`

This file defines the recurring review that keeps `.claude/` itself healthy: agents, skills, workflows, templates, hooks/settings, and the core docs (root `CLAUDE.md`, `README.md`, `AI_CONTEXT.md`, `WORKFLOWS.md`). It is a **meta-maintenance** doc — it reviews the Claude configuration, not the application code (see [workflows/sync-documentation.md](workflows/sync-documentation.md) for that).

**ROT** = the three things a review looks for:

- **R — Redundant**: two agents/skills/workflows with overlapping responsibility; the same guidance repeated in multiple files instead of cross-linked; a template duplicating another.
- **O — Outdated**: content that no longer matches reality — a broken cross-reference, a described project/type/path that no longer exists, a convention the code has moved away from, a stack claim in `CLAUDE.md` that has drifted.
- **T — Trivial**: an agent/skill/workflow that's never invoked, adds no value over Claude reasoning inline, or has shrunk to boilerplate that restates the obvious — including config about things that don't exist in this repository.

ROT review is **explicit and pull-based** — run it when the user asks, not as a side effect of other work (see `CLAUDE.md` §2.5). Findings are reported; fixes require the same confirm-before-edit discipline as [sync-documentation](workflows/sync-documentation.md).

## Schedule

| Cadence | Trigger | Scope |
|---|---|---|
| Monthly (or every ~4–6 weeks of active work) | Manual — user runs it | Full `.claude/` sweep |
| After adding/removing/renaming any project (framework, host, module, or test) | Event-based | Root `CLAUDE.md` §1/§3, `AI_CONTEXT.md`, and any agent/skill/template naming projects or paths |
| After a stack/convention change (target framework, EF Core provider set, vendor `Lightsoft.*` package family, test stack, result/validation pattern; Next.js major version or client-app structural conventions) | Event-based | Root `CLAUDE.md` § Framework Conventions and the agents that bake in specifics (`dotnet-developer`, `ddd-modeler`, `efcore-specialist`, `api-designer`, `testing-reviewer`; `nextjs-developer`, `nextjs-architect` for the client app) |
| When further client apps or business modules are added to this branch | Event-based | Reintroduce only the agents/skills/workflows that the new code actually needs — don't pre-create them |
| Before a release/milestone tag | Event-based | Full sweep, emphasis on Trivial |

Track actual runs in the [Review Log](#review-log) below so the next review knows the baseline.

## How to Run It

Ask Claude: *"Run a ROT review of `.claude/` per ROT.md"* (optionally scoped). Expected behavior:

1. Read the [Review Log](#review-log) to find the last reviewed date/scope — a review is a diff since then, not necessarily a full re-read.
2. Walk the checklist below for the scope in play.
3. Report findings as a table: file → R/O/T → what's wrong → suggested action. No file edits during this step.
4. Apply only the fixes the user explicitly approves.
5. Append a row to the Review Log once findings are reported (or fixes applied, if asked).

## Checklist

### Core docs (root `CLAUDE.md`, `README.md`, `AI_CONTEXT.md`, `WORKFLOWS.md`)

- [ ] Every agent/skill/workflow link in root `CLAUDE.md` §4–6/§9 and `WORKFLOWS.md` resolves to a file that still exists, and every existing agent/skill/workflow is listed.
- [ ] Root `CLAUDE.md` §1/§3 and § Framework Conventions still match what's in `src/`, `tests/`, `clients/`, and the central build files.
- [ ] No core doc contradicts another, and no fact is stated in two homes.
- [ ] Core-doc footers stay a single `_Last synced: <date>_` line.

### Agents (`agents/*.md`)

- [ ] Each agent is referenced by at least one skill or workflow, or by `CLAUDE.md` §4 — an orphan is a Trivial candidate.
- [ ] No two agents claim overlapping primary responsibility without a clear "defer to X for Y" boundary.
- [ ] `description` frontmatter still scopes the agent accurately and names sibling agents by current filename.
- [ ] `dotnet-developer` and `nextjs-developer` (the only agents with `Edit`/`Write` on code) still state in both `description` and body that they run **only after a plan is approved**, do build-sanity but **never run the test suite**, and never edit docs as a side effect.
- [ ] Design agents (`dotnet-architect`, `ddd-modeler`, `api-designer`, `efcore-specialist`, `nextjs-architect`) and review agents (including `frontend-code-reviewer`, `api-contract-reviewer`) don't gain write tools or drift into implementation.
- [ ] Baked-in repo specifics (project names, `DomainEvent`/`ValueObject` bases, `DbProvider` set, controller bases, xunit.v3 + Moq; the client app's `lib/server/` files, barrel/`<feature>.api.ts` conventions, `pnpm` scripts) still match the code.

### Skills (`skills/<name>/SKILL.md`)

- [ ] Every skill is a folder `skills/<name>/SKILL.md` whose frontmatter `name` matches the folder and has a `description`.
- [ ] Each skill is referenced from `CLAUDE.md` §5, a workflow, or another skill.
- [ ] Workflow steps name agents/skills that exist under their current filenames, and step numbers cited across files (e.g. `create-feature` → `implement-feature`) still line up.
- [ ] No two skills cover the same task with diverging advice.

### Workflows (`workflows/*.md`)

- [ ] `WORKFLOWS.md` index matches the actual files in `workflows/`.
- [ ] No workflow merely restates a skill (Redundant) — the skill owns the procedure.

### Doc templates (`.claude/docs/templates/*.md`)

- [ ] Every template is used by at least one skill/agent/workflow, and its "Used by" header is accurate.
- [ ] `Output location` comments match the root `docs/{architecture,conventions}/` layout, or `clients/<app-name>/docs/` for client-app templates.
- [ ] No template duplicates another's structure.

### Hooks & settings (`settings.json`, `hooks/*.js`)

- [ ] Every hook command in `settings.json` references a script under `hooks/` that exists, and `settings.json` is valid JSON.
- [ ] `hooks/context-recap.js`'s behavior matches `AI_CONTEXT.md` § Automatic Context Recap.

### Model currency

- [ ] No agent frontmatter pins a `model:` without a deliberate, still-valid reason.
- [ ] No agent/skill/workflow hardcodes a specific model name or a workaround tied to an old model's limitations.

## Review Log

| Date | Scope | Reviewer | R/O/T found | Actions taken |
|---|---|---|---|---|
| 2026-09-29 | Full `.claude/` + root `CLAUDE.md` + `README.md`, for the `dev/core` branch (framework projects + `tests/Framework.Tests` only; no modules, hosts, or clients) | Claude + user | Trivial: every agent/skill/workflow/template whose purpose was client apps, the MVC host, API-contract drift, full-stack features, business-module analysis/splitting, or project scaffolding. Outdated: remaining files pointed at `clients/`, module docs, `src/CLAUDE.md`/`src/docs/` content, and module-specific conventions absent here. Redundant: the analyze-solution workflow restated its skill. | Deleted the Trivial/Redundant files; rewrote the remaining agents, skills, workflows, templates, and core docs for the framework layer; inlined the essential conventions into root `CLAUDE.md` § Framework Conventions; reset this log to a `dev/core` baseline (earlier history lives in git on `main`). |
| 2026-09-30 | Event-based (projects added: Host, Identity/.Contracts/.Web, migrators, Identity.Tests; docs moved to root `docs/`): full `.claude/` + root `CLAUDE.md`/`README.md` | Claude + user | Outdated: `context-recap.js` emitted `hookSpecificOutput` for `PostCompact` (rejected by Claude Code); framework-only scope wording in several agents/skills; nonexistent `tests/<Project>.Tests` path; missing migrator package-version opt-out; `overview.md`/`database.md` template targets; WORKFLOWS.md footer. Redundant: dependency-direction rules restated in agents; AI_CONTEXT safety rules; sync-docs skill restating its workflow. Trivial: `solution-overview`/`database` templates; stale `settings.local.json` entries. | Moved the recap hook to `SessionStart[resume\|compact]`; widened agent/skill scopes to host and modules; replaced restated rules with links to `CLAUDE.md` §1; deleted the two unused templates; thinned `sync-docs`; documented the migrator package opt-out; fixed the test-runner wording in coding-conventions/README; pruned `settings.local.json`. |
| 2026-09-30 | Event-based (client app `clients/admin/` and `docs/integration.md` added): frontend agents/skills/workflows ported from `main` | Claude + user | Outdated: `AI_CONTEXT.md` and several workflows/skills stated there are no client apps. Missing: client-app design, implementation, review, and contract-drift coverage. | Reintroduced `nextjs-architect`, `nextjs-developer`, `frontend-code-reviewer`, `api-contract-reviewer`, skills `nextjs`/`analyze-client`/`context-frontend`/`create-feature`, and the `client-app-overview` template, adapted to `clients/admin/` and the flat backend; added client routing to the workflows and to the `api`/`generate-docs`/`sync-docs`/`review-code`/`refactor`/`testing`/`performance` skills and `documentation-writer`. Not reintroduced (single client app): `analyze-frontend`, `frontend-overview` template, `context-backend`/`context-full`. |

_This file is itself subject to ROT review — if the schedule or checklist stops matching how the project works, update it during a review rather than letting it drift._

---
_Last synced: 2026-09-30_
