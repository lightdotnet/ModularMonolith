<!--
Template: Dependency Graph
Used by: agents/dependency-analyzer.md, skills/generate-docs/SKILL.md
Output location: src/docs/architecture/dependency-graph.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Dependency Graph

## Project References

_The project-to-project reference diagram — the one canonical home for it._

## Package References

Group by project, noting what each package is for. Do not duplicate version numbers that live in `Directory.Packages.props` — point to that file instead. Only call out a specific version inline when it's the fact being reported (e.g. a `Version=` override outside central management).

## Circular References

_List any found — always worth flagging, never expected._

## Version Mismatches

_Same package pinned to different versions, or versions set outside central package management. State "none found" rather than deleting the section._

## Direction Violations

_Any reference that breaks the framework's dependency direction (e.g. `Shared` referencing another solution project, a framework project referencing a business module)._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
