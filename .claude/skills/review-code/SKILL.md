---
name: review-code
description: Playbook for reviewing specific code, diffs, or PRs for correctness and quality using code-reviewer (backend) or frontend-code-reviewer (a client app).
---

# Skill: Review Code

## Purpose

Provide a focused, read-only quality review of specific code — a diff, a file, or a named set of changes — without expanding into architecture, security, or performance analysis (those have dedicated skills/agents).

## Inputs

- The specific code/diff/PR to review (ask if not specified — never review "the whole repo" under this skill).
- Optional: known conventions from `src/docs/conventions/coding-conventions.md` (backend) or `clients/<app-name>/docs/conventions/coding-conventions.md` (client app) for the relevant scope.

## Workflow

1. **Scope the review**: identify exactly which files/diff are in scope, which stack they're on, and (if a client app) which one under `clients/`.
2. **Delegate**: invoke [code-reviewer](../agents/code-reviewer.md) for backend (`src/`) files, or [frontend-code-reviewer](../agents/frontend-code-reviewer.md) for a client app's (`clients/<app-name>/`) files. If the diff spans both, invoke each for its portion.
3. **Cross-reference conventions**: check `src/docs/conventions/coding-conventions.md` (backend) or `clients/<app-name>/docs/conventions/coding-conventions.md` (client app) for the relevant scope, if populated, to judge consistency rather than impose generic style opinions.
4. **Escalate if needed**: if the review surfaces architecture, security, or performance concerns beyond line-level quality, note them briefly and suggest the matching specialized skill/agent rather than going deep here.
5. **Report**: present findings ranked by importance, with file:line references and concrete suggested fixes.

## Expected Outputs

- A ranked list of findings (correctness > maintainability > style).
- A short overall verdict.
- Pointers to other skills/agents for any out-of-scope concerns found along the way.

## Best Practices

- Don't rewrite code as part of a review — suggest, then let the user decide whether to apply changes.
- Don't review files outside the requested scope.
- Respect existing local conventions over generic "best practice" opinions when they conflict.
