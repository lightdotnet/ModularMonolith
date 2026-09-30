# Standard Claude Workflows — Index

This file indexes the workflows in [workflows/](workflows/). Workflows are step-by-step session procedures — heavier-weight than a [skill](skills/), typically spanning a whole session or a whole multi-step request.

| Workflow | Trigger | Modifies code? | Modifies docs? |
|---|---|---|---|
| [new-session](workflows/new-session.md) | Start of every session | No | No |
| [analyze-folder](workflows/analyze-folder.md) | "Analyze this folder" | No | Only the scoped folder's doc, if asked |
| [implement-feature](workflows/implement-feature.md) | "Implement/add/change X" | Yes (after plan approval) | Only if requested |
| [review-repository](workflows/review-repository.md) | "Review the repository/codebase" | No (read-only) | No |
| [sync-documentation](workflows/sync-documentation.md) | "Sync/update documentation" | No | Yes |
| [end-session](workflows/end-session.md) | End of a non-trivial session | No | No (suggests only) |

## Selection Guide

- Starting fresh → `new-session`
- User names a specific folder → `analyze-folder`; a project → [skills/analyze-project](skills/analyze-project/SKILL.md); the whole solution → [skills/analyze-solution](skills/analyze-solution/SKILL.md)
- User wants new or changed code → `implement-feature`
- User wants a broad, read-only audit → `review-repository`
- User explicitly wants docs regenerated/updated to match code → `sync-documentation`
- Wrapping up a session with meaningful changes → `end-session`

## Relationship to Skills and Agents

- **Workflows** orchestrate a request end-to-end (may invoke multiple skills/agents in sequence).
- **Skills** ([skills/](skills/)) are focused, reusable playbooks for one kind of task.
- **Agents** ([agents/](agents/)) are specialized designers, an implementer, reviewers, and analysts invoked by workflows or skills — the design/review ones keep deep investigation out of the main context; `dotnet-developer` makes the code changes once a plan is approved.

---
_Last synced: 2026-09-30_
