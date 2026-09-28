---
name: sync-docs
description: Playbook for updating existing generated documentation to match the current codebase, preserving manual content and removing stale sections.
---

# Skill: Sync Docs

## Purpose

Bring existing documentation under `src/docs/` and `clients/<app-name>/docs/` (and, if requested, `src/CLAUDE.md`/`clients/<app-name>/CLAUDE.md`/the root `CLAUDE.md`/`docs/integration.md`) up to date with the current code — additive, corrective, and stale-content-removing, never a blind rewrite.

## Inputs

- The scope to sync (a specific backend module/project/folder, a frontend area, or explicitly "all docs" if the user really means that).
- The existing doc(s) to sync against.

## Workflow

1. **Scope**: confirm exactly which docs are being synced. Do not sync docs outside the requested scope.
2. **Read current doc**: load the existing generated doc content for the scope.
3. **Read current code**: inspect the actual current state of the scoped module/project/folder/frontend area.
4. **Diff**: identify what changed (new facts), what's now stale (no longer true), and what's unaffected.
5. **Present one consolidated summary and get one approval**: list every file that will change and a short summary of what changes in each, then ask for a single approval covering the whole batch — never one approval per file.
6. **Preserve manual content**: any clearly human-authored section (e.g. a "Notes" section, an explicit manual marker) must be kept verbatim.
7. **Delegate the write**: invoke [documentation-writer](../agents/documentation-writer.md) to apply the update.
8. **Report**: summarize what was added, updated, and removed.

## Expected Outputs

- A pre-write summary (files + planned changes) with a single approval gate before any file is touched.
- Updated doc file(s) reflecting current code, with manual content intact.
- A changelog-style summary of the sync (added/updated/removed).

## Best Practices

- Never run this as a side effect of an unrelated task — only on explicit request.
- Removing stale content is expected and desired — don't leave contradictions between docs and code.
- If it's unclear whether a section is manual or previously generated, ask rather than guessing and potentially deleting someone's manual notes.
- Write present-tense facts only — never narrate the sync itself into the doc body ("new this sync"/"changed this sync"/"unchanged" markers, chained multi-date footers). Every file ends in one `_Last synced: <date>_` line; the changelog-style summary of what changed belongs in the report to the user, not in the file.
- Don't duplicate a project-reference/import diagram or exact package version numbers across multiple docs — `dependency-graph.md` is the one canonical home for that (see its template); other docs link to it with a short summary instead of repeating it. Point at the version-of-record file (`Directory.Packages.props` for the backend, `package.json` for a client app) rather than copying exact version numbers into prose, since those go stale on every bump.
- A single-project backend module gets a flat `src/docs/architecture/modules/<ModuleName>.md`; only use the nested `modules/<ModuleName>/overview.md` form for a split (Domain/Application/Infrastructure/Api) module — see `docs/templates/module-overview.md`.
