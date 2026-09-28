<!--
Template: Database
Used by: skills/generate-docs/SKILL.md, skills/sync-docs/SKILL.md
Output location: src/docs/architecture/database.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Database: Backend

## DbContext(s) per Module

| Module | DbContext | Provider | Notes |
|---|---|---|---|

## Entities

| Entity | Table | Owning module | Notes |
|---|---|---|---|

## Relationships

_Key relationships between entities, verified from actual configuration (Fluent API/attributes), not assumed. Flag any relationship that crosses module boundaries directly._

## Migration Strategy

_How migrations are created/applied per module (e.g. `dotnet ef migrations`, custom tooling)._

## Migration History (high-level)

| Module | Migration | Date | Summary | Destructive? |
|---|---|---|---|---|

## Indexing / Performance Notes

_Only verified/observed facts — indexes actually configured, known slow queries with evidence._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
