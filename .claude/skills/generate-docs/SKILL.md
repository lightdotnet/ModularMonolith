---
name: generate-docs
description: Playbook for generating new documentation from code for a specific scope — a backend project, the solution, or the client app under clients/admin/ — using the templates in .claude/docs/templates/.
---

# Skill: Generate Docs

## Purpose

Produce new documentation for a specific project, the solution, or the client app that doesn't yet have generated docs, using the standard templates — never speculative.

## Inputs

- The target scope (the solution, one project, or the client app).
- Which doc type is wanted (project overview, client-app overview, architecture, domain model, coding conventions, dependency graph, development guide) — see [.claude/docs/templates/](../../docs/templates/).

## Workflow

1. **Scope and doc type**: confirm exactly what's being documented and which template applies.
2. **Read the template**: load the matching file from `.claude/docs/templates/` for structure. For the client app, the overview uses [client-app-overview](../../docs/templates/client-app-overview.md); architecture, dependency-graph, coding-conventions, and development-guide reuse the backend templates' structure, dropping sections that only apply to .NET projects.
3. **Delegate**: invoke [documentation-writer](../../agents/documentation-writer.md) with the scope and template.
4. **Verify facts**: every claim must trace to actual code inspected for this scope — mark anything unverifiable as `unknown` rather than guessing.
5. **Write output**: each side owns its own docs folder.
   - Backend: the root `docs/architecture/<doc-type>.md` (architecture, dependency-graph), `docs/architecture/projects/<ProjectName>.md` (per-project overviews), or `docs/conventions/<doc-type>.md` (coding-conventions, development-guide).
   - Client app: `clients/<app-name>/docs/architecture/<doc-type>.md` (overview, architecture, dependency-graph) or `clients/<app-name>/docs/conventions/<doc-type>.md`.
   - Facts spanning both sides: root `docs/integration.md`.
6. **Report**: summarize what was generated and flag any gaps found.

## Expected Outputs

- A new markdown file in the scope's docs folder following the template structure, populated only with verified facts.
- A short summary of what was generated and any open questions.

## Best Practices

- Don't generate docs for scopes not requested, even if adjacent.
- If a doc already exists for this scope, use [sync-docs](../sync-docs/SKILL.md) instead of overwriting it.
- Follow the doc-writing rules in [sync-documentation](../../workflows/sync-documentation.md) — they apply to a first-time write too.
