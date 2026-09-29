# Workflow: End Session

Run at the end of a non-trivial session (meaningful code or doc changes were made).

## Steps

1. **Summarize completed work.** A concise account of what changed, scoped to what actually happened this session.
2. **Suggest documentation updates.** If code changed in ways that would make root `CLAUDE.md`, `README.md`, or generated docs under `src/docs/` stale, name specifically which doc(s) and why.
3. **Flag public-API impact.** If a public type/member in a framework project changed, note it as a potential breaking change for consuming modules.
4. **List follow-up tasks.** Anything identified but not done (e.g. a review finding not yet fixed, a test gap noted but not closed, a decision still pending).
5. **Do not modify documentation.** Suggesting is the end of this workflow's responsibility — syncing requires the user to invoke [sync-documentation](sync-documentation.md).

## Output

- A short summary of completed work.
- A list of suggested documentation updates (not applied).
- A list of follow-up tasks/open questions.
