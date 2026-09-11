# Standard Claude Workflows — Index

This file indexes the workflows in [workflows/](workflows/). Workflows are step-by-step session procedures — heavier-weight than a [skill](skills/), typically spanning a whole session or a whole multi-step request.

| Workflow | Trigger | Modifies code? | Modifies docs? |
|---|---|---|---|
| [new-session](workflows/new-session.md) | Start of every session | No | No |
| [scaffold-project](workflows/scaffold-project.md) | "Scaffold the project," "set up the initial backend/frontend" | Yes (after plan approval) | Only if requested |
| [analyze-folder](workflows/analyze-folder.md) | "Analyze this folder" | No | Only the scoped folder's doc, if asked |
| [analyze-solution](workflows/analyze-solution.md) | "Analyze the backend/solution" | No | Only the backend doc, if asked |
| [implement-feature](workflows/implement-feature.md) | "Implement/add a feature" | Yes (after plan approval) | Only if requested |
| [review-repository](workflows/review-repository.md) | "Review the repository/codebase" | No (read-only) | No |
| [sync-documentation](workflows/sync-documentation.md) | "Sync/update documentation" | No | Yes |
| [end-session](workflows/end-session.md) | End of a non-trivial session | No | No (suggests only) |

## Selection Guide

- Starting fresh, don't know where to begin → `new-session`
- Repo (or a part of it) has no code yet and the user wants the backend/frontend skeleton created → `scaffold-project`
- User names a specific folder → `analyze-folder`
- User wants the backend solution understood → `analyze-solution` (see also [skills/analyze-module.md](skills/analyze-module.md) for a single module)
- User wants a client app understood → [skills/analyze-frontend.md](skills/analyze-frontend.md) for all of `clients/`, [skills/analyze-client.md](skills/analyze-client.md) for one named app
- User wants new behavior/code (backend, frontend, or both) → `implement-feature`
- User wants a broad, read-only audit across the repo → `review-repository`
- User explicitly wants docs regenerated/updated to match code → `sync-documentation`
- Wrapping up a session with meaningful changes → `end-session`

## Relationship to Skills and Agents

- **Workflows** orchestrate a request end-to-end (may invoke multiple skills/agents in sequence).
- **Skills** ([skills/](skills/)) are focused, reusable playbooks for one kind of task.
- **Agents** ([agents/](agents/)) are specialized designers, implementers, reviewers, and analysts invoked by workflows or skills — the design/review ones keep deep investigation out of the main context; `dotnet-developer`/`nextjs-developer` do the actual code changes once a plan is approved.

A workflow typically: loads minimal context → selects skills/agents → executes → reports back, without expanding scope beyond what was asked.
