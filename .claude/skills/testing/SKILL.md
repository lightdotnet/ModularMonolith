---
name: testing
description: Playbook for reviewing or improving test coverage and quality on either stack using the testing-reviewer agent.
---

# Skill: Testing

## Purpose

Assess and improve test coverage/quality for specific backend or frontend code, focused on behaviorally meaningful gaps rather than raw coverage percentage.

## Inputs

- The target code/module/frontend area to assess.
- Whether the ask is a review only, or review plus adding tests.

## Workflow

1. **Scope**: identify the specific code/module/frontend area whose tests are in question.
2. **Delegate review**: invoke [testing-reviewer](../agents/testing-reviewer.md) for a coverage/quality assessment.
3. **Prioritize public surface**: on the backend, prioritize coverage of a module's `Api`/`Application` entry points; on a client app, prioritize components/hooks with real logic over presentational ones.
4. **If adding tests**: implement the highest-priority missing cases first, following existing test conventions (framework, naming, mocking style) rather than introducing a second stack (e.g. don't add Vitest alongside an existing Jest setup without reason).
5. **Report**: coverage summary, prioritized gaps, and (if implemented) what was added.

## Expected Outputs

- A coverage/quality assessment with concrete, scenario-level gaps.
- Optionally, new/updated tests closing the highest-priority gaps.

## Best Practices

- Don't chase 100% coverage as a goal — prioritize behavior that matters, especially public/module contracts.
- Match existing test framework/conventions on the relevant side; don't introduce a second testing stack.
- Flag brittle/flaky existing tests rather than silently working around them.
