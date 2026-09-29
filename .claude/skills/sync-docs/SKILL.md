---
name: sync-docs
description: Playbook for updating existing documentation (generated docs under src/docs/, root CLAUDE.md, README.md, or .claude/ docs) to match the current codebase, preserving manual content and removing stale sections.
---

# Skill: Sync Docs

## Purpose

Bring existing documentation — generated docs under `src/docs/`, and, if requested, root `CLAUDE.md`, `README.md`, or the `.claude/` docs — up to date with the current code: additive, corrective, and stale-content-removing, never a blind rewrite.

## Inputs

- The scope to sync (a specific project/doc, or explicitly "all docs" if the user really means that).
- The existing doc(s) to sync against.

## Workflow

1. **Scope**: confirm exactly which docs are being synced.
2. **Read current doc** and **current code** for the scope.
3. **Diff**: identify what changed (new facts), what's now stale, and what's unaffected.
4. **Get one batch approval**: per [sync-documentation](../../workflows/sync-documentation.md) step 3.
5. **Preserve manual content**: any clearly human-authored section must be kept verbatim.
6. **Delegate the write**: invoke [documentation-writer](../../agents/documentation-writer.md) to apply the update.
7. **Report**: summarize what was added, updated, and removed.

## Expected Outputs

- A pre-write summary (files + planned changes) with a single approval gate.
- Updated doc file(s) reflecting current code, with manual content intact.
- A changelog-style summary of the sync.

## Best Practices

- Never run this as a side effect of an unrelated task — only on explicit request.
- If it's unclear whether a section is manual or generated, ask rather than guessing.
- Follow the doc-writing rules in [sync-documentation](../../workflows/sync-documentation.md).
