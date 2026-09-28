<!--
Template: Dependency Graph
Used by: agents/dependency-analyzer.md
Output location: src/docs/architecture/dependency-graph.md (or clients/<app-name>/docs/architecture/dependency-graph.md)
Do not populate this file itself — copy its structure into the generated output.
-->

# Dependency Graph: <Backend | Client app name>

## Package References

Group by project (backend) or by purpose (client app), noting what each package is for. Do not duplicate exact version numbers here if they live in a single central file (`Directory.Packages.props` for the backend, `package.json` for a client app) — point to that file instead; a hardcoded version table goes stale on every bump. Only call out a specific version inline when it's the fact being reported (e.g. an intentional pin below the family's shared version).

## Circular References

_List any found — these are always worth flagging, never expected. Include project-to-project (backend) or internal-module-import (client app) direction only where it's needed to explain a finding — this section reports the verification result, not a full edge list._

## Version Mismatches

_Same package pinned to different versions across modules (backend) or in package.json/lockfile (client app) — or across sibling client apps, if they're meant to share versions. State "not applicable" rather than deleting the section if nothing to report._

## Cross-Module Boundary Violations (backend only)

_Any module referencing a project of another module other than that module's `<Module>.Contracts` project — always worth flagging. State "not applicable" for a client-app dependency graph._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
