---
name: sync-docs
description: Playbook for updating existing documentation (generated backend docs under the root docs/ folder, the client app's docs under clients/admin/docs/, docs/integration.md, root CLAUDE.md, clients/admin/CLAUDE.md, README.md, or .claude/ docs) to match the current codebase, preserving manual content and removing stale sections.
---

# Skill: Sync Docs

## Purpose

Bring existing documentation — generated backend docs under the root `docs/` folder, the client app's docs under `clients/admin/docs/`, the boundary doc `docs/integration.md`, and, if requested, root `CLAUDE.md`, `clients/admin/CLAUDE.md`, `README.md`, or the `.claude/` docs — up to date with the current code. Only on explicit request, never as a side effect.

## Inputs

- The scope to sync (a specific project/doc, the client app's docs, or explicitly "all docs" if the user really means that).
- The existing doc(s) to sync against.

## Procedure

Follow [sync-documentation](../../workflows/sync-documentation.md) — it owns the procedure and the doc-writing rules.

## Expected Outputs

- A pre-write summary (files + planned changes) with a single approval gate.
- Updated doc file(s) reflecting current code, with manual content intact.
- A changelog-style summary of the sync.
