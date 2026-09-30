<!--
Template: Database
Used by: skills/generate-docs/SKILL.md, skills/sync-docs/SKILL.md
Output location: docs/architecture/database.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Database

## Base DbContext

_What the base context provides to every module's own context (audit, domain-event dispatch, conventions) — verified from code._

## Providers

| Provider | Package | Notes (provider-specific behavior/limitations) |
|---|---|---|

## Shared Entity & Index Conventions

_Base entity types, builder extensions, and configuration conventions modules are expected to follow._

## Repositories & Caching

_Repository/cache building blocks and when to use each._

## Migration Support

_How migrations are created/applied (tooling, design-time support), verified._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
