---
name: testing
description: Playbook for reviewing or improving test coverage and quality of the framework projects (tests/Framework.Tests) and the Identity module (tests/Identity.Tests) using the testing-reviewer agent.
---

# Skill: Testing

## Purpose

Assess and improve test coverage/quality for specific framework code, focused on behaviorally meaningful gaps rather than raw coverage percentage.

## Inputs

- The target code/project to assess.
- Whether the ask is a review only, or review plus adding tests.

## Workflow

1. **Scope**: identify the specific code whose tests are in question.
2. **Delegate review**: invoke [testing-reviewer](../../agents/testing-reviewer.md) for a coverage/quality assessment.
3. **Prioritize public surface**: base types, extension methods, and behaviours modules depend on; provider-sensitive persistence behavior.
4. **If adding tests**: propose the highest-priority missing cases as a plan and wait for approval (root `CLAUDE.md` §2.9); once approved, hand the writing to [dotnet-developer](../../agents/dotnet-developer.md), following the existing xunit.v3 + Moq conventions. Run the test suite only when the user explicitly asks.
5. **Report**: coverage summary, prioritized gaps, and (if implemented) what was added.

## Expected Outputs

- A coverage/quality assessment with concrete, scenario-level gaps.
- Optionally, new/updated tests closing the highest-priority gaps.

## Best Practices

- Don't chase 100% coverage — prioritize behavior that matters.
- Don't introduce a second testing stack.
- Flag brittle/flaky existing tests rather than silently working around them.
