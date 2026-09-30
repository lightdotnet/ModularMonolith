# Workflow: New Session

Run this at the start of every session in this repository.

## Steps

1. **Read the root `CLAUDE.md`.** It is the source of truth for operating rules, conventions, and the agent/skill/workflow index. Do not skip even if this feels like a quick task.
2. **Do not read anything else yet.** Wait for the user's actual request before loading further context.
3. **Once the request is known, load only required context**:
   - If it names a project/folder, jump straight there (`Glob` for the relevant `.csproj` or folder, or a route/feature folder under `clients/admin/src/`; don't tree-walk the repo).
   - If it's ambiguous which project or client-app area applies, ask rather than scanning broadly.
   - Check generated docs for already-verified facts before re-deriving them from code: `docs/` for the backend, `clients/admin/CLAUDE.md` and `clients/admin/docs/` for the client app (or load them in one step with [context-frontend](../skills/context-frontend/SKILL.md)), `docs/integration.md` for the boundary between them.
4. **Identify relevant agents** from root `CLAUDE.md` §4 that match the request's domain — plan to delegate rather than doing deep multi-domain analysis inline. For an approved code change, the implementation itself goes to `dotnet-developer` (backend) or `nextjs-developer` (client app).
5. **Identify relevant skills/workflows** from `.claude/WORKFLOWS.md` and root `CLAUDE.md` §9.
6. **Read only the necessary files** for the identified scope — no speculative exploration.

## Output

Nothing to report yet — this workflow establishes context before the requested work. Proceed directly into the matched skill/workflow/agent.
