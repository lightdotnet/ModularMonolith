# Workflow: Analyze Solution

Triggered by requests like "analyze the backend/solution" or "analyze the solution."

## Steps

1. **Confirm this means the backend.** If the user might mean a client app instead, confirm (`clients/` → [analyze-frontend skill](../skills/analyze-frontend/SKILL.md) for the whole folder, [analyze-client skill](../skills/analyze-client/SKILL.md) for one named app).
2. **Run the [analyze-solution skill](../skills/analyze-solution/SKILL.md).** It owns the procedure — locating the solution, the dependency map, the optional structural read, and the documentation step (only when explicitly requested).

## Output

- See the [analyze-solution skill](../skills/analyze-solution/SKILL.md) § Expected Outputs.
