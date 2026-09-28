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
5. **Get one batch approval**: per [sync-documentation](../../workflows/sync-documentation.md) step 3.
6. **Preserve manual content**: any clearly human-authored section (e.g. a "Notes" section, an explicit manual marker) must be kept verbatim.
7. **Delegate the write**: invoke [documentation-writer](../../agents/documentation-writer.md) to apply the update.
8. **Report**: summarize what was added, updated, and removed.

## Expected Outputs

- A pre-write summary (files + planned changes) with a single approval gate before any file is touched.
- Updated doc file(s) reflecting current code, with manual content intact.
- A changelog-style summary of the sync (added/updated/removed).

## Best Practices

- Never run this as a side effect of an unrelated task — only on explicit request.
- Removing stale content is expected and desired — don't leave contradictions between docs and code.
- If it's unclear whether a section is manual or previously generated, ask rather than guessing and potentially deleting someone's manual notes.
- Follow the doc-writing rules in [sync-documentation](../../workflows/sync-documentation.md) (no sync narration and a single `_Last synced_` footer, `dependency-graph.md` as the canonical diagram home, version-of-record files over copied versions, flat vs. nested module docs).
