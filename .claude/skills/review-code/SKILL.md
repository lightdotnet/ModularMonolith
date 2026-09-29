---
name: review-code
description: Playbook for reviewing specific C# code, diffs, or PRs for correctness and quality using code-reviewer.
---

# Skill: Review Code

## Purpose

Provide a focused, read-only quality review of specific code — a diff, a file, or a named set of changes — without expanding into architecture, security, or performance analysis (those have dedicated agents).

## Inputs

- The specific code/diff/PR to review (ask if not specified — never review "the whole repo" under this skill).

## Workflow

1. **Scope the review**: identify exactly which files/diff are in scope.
2. **Delegate**: invoke [code-reviewer](../../agents/code-reviewer.md).
3. **Judge against local conventions**: root `CLAUDE.md` § Framework Conventions and the surrounding code, rather than generic style opinions.
4. **Escalate if needed**: note architecture, security, or performance concerns briefly and suggest the matching agent rather than going deep here.
5. **Report**: findings ranked by importance, with file:line references and concrete suggested fixes.

## Expected Outputs

- A ranked list of findings (correctness > maintainability > style).
- A short overall verdict.
- Pointers to other agents for any out-of-scope concerns.

## Best Practices

- Don't rewrite code as part of a review — suggest, then let the user decide.
- Don't review files outside the requested scope.
