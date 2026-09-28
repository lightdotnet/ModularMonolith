---
name: generate-docs
description: Playbook for generating new documentation from code for a specific backend or frontend scope, using the templates in .claude/docs/templates/.
---

# Skill: Generate Docs

## Purpose

Produce new documentation for a specific module/project/frontend area that doesn't yet have generated docs, using the standard templates — never repo-wide, never speculative.

## Inputs

- The target scope (backend module/project, or a frontend area) to document.
- Which doc type is wanted (overview, module overview, frontend overview, architecture, API, database, domain model, conventions, dependency graph, dev guide) — see [docs/templates/](../docs/templates/).

## Workflow

1. **Scope and doc type**: confirm exactly what's being documented and which template applies.
2. **Read the template**: load the matching file from `.claude/docs/templates/` for structure.
3. **Delegate**: invoke [documentation-writer](../agents/documentation-writer.md) with the scope and template.
4. **Verify facts**: every claim in the generated doc must trace to actual code inspected for this scope — mark anything unverifiable as `unknown` rather than guessing.
5. **Write output**: each project owns its own `docs/` — for the backend, place the result under `../../src/docs/architecture/<doc-type>.md` (overview, architecture, dependency-graph, `modules/<ModuleName>.md`) or `../../src/docs/conventions/<doc-type>.md` (coding-conventions, development-guide); for a client app, under `../../clients/<app-name>/docs/architecture/<doc-type>.md` or `../../clients/<app-name>/docs/conventions/<doc-type>.md` accordingly. There is no shared `docs/generated/` staging area — don't write there.
6. **Report**: summarize what was generated and flag any gaps found during generation.

## Expected Outputs

- A new markdown file under `src/docs/` or `clients/<app-name>/docs/` following the template structure, populated only with verified facts.
- A short summary of what was generated and any open questions.

## Best Practices

- Don't generate docs for scopes not requested, even if adjacent.
- Don't carry over assumptions from similar modules — verify per-scope.
- If a doc already exists for this scope, use [sync-docs](sync-docs.md) instead of blindly overwriting it.
- Write present-tense facts only — a freshly generated doc has no sync history to narrate, so there's nothing to say beyond a single `_Last synced: <date>_` footer; don't invent "new"/"changed" framing for a first-time write.
- Don't duplicate a project-reference/import diagram or exact package version numbers across multiple docs — put that in `dependency-graph.md` (its template) and have `overview.md`/`architecture.md` link to it instead.
- A single-project backend module gets a flat `src/docs/architecture/modules/<ModuleName>.md`; only use the nested `modules/<ModuleName>/overview.md` form for a split (Domain/Application/Infrastructure/Api) module.
