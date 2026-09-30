# Workflow: Sync Documentation

Triggered only by an explicit request to sync/update documentation. This workflow is the canonical home for the doc-writing rules below; [sync-docs](../skills/sync-docs/SKILL.md) and [generate-docs](../skills/generate-docs/SKILL.md) link here rather than repeating them.

## Steps

1. **Confirm scope.** Which project/folder's docs are being synced, or is it genuinely all of them? Don't assume "all" unless the user says so.
2. **Diff each doc in scope**: delegate to [documentation-writer](../agents/documentation-writer.md) (or use facts already verified earlier in the session) to compare the existing doc against current code and determine what's added/updated/removed.
3. **Present one consolidated summary and get one approval.** Before writing anything, list every file that will change and, per file, a short summary of the change. Ask for a single approval covering the whole batch — never file-by-file.
4. **Apply all approved changes** without asking again per file. For a file needing more than one change, write the complete file in one pass and read it back, rather than chaining many partial edits.
5. **Preserve manual documentation**: clearly human-authored content (explicit "Notes"/manual sections, or content not previously generated) is kept verbatim.
6. **Remove outdated information**: stale facts are removed, not left contradicting reality.
7. **Keep docs consistent and non-duplicated**: if a change in one doc affects another in scope, update both. One fact has one home — link to it rather than repeating it. `dependency-graph.md` is the canonical home for the project-reference diagram and its circular-reference/direction checks; `architecture.md` links to it. Point at the version-of-record file (`Directory.Packages.props`) rather than copying package versions into prose.
8. **Stay at stable-structure altitude**: describe structure, rules, and responsibilities — no feature inventories or file censuses that go stale on every change.
9. **Write present-tense facts only — never narrate the sync itself.** No "new this sync"/"changed"/"previously" markers in file content, and every synced file ends in exactly one `_Last synced: <date>_` line, replacing whatever was there before. The changelog belongs in the chat report (step 10), not in the file.
10. **Report**: a changelog-style summary of what was added, updated, and removed, per doc.

## Output

- A pre-write summary (files + planned per-file changes) with a single approval gate before any file is touched.
- Updated documentation files, scoped to what was confirmed, with manual content preserved, narration-free, and each ending in a single `_Last synced: <date>_` line.
- A summary of the sync (added/updated/removed) for the user to review.
