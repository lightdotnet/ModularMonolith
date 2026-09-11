<!--
Template: Domain Model / Aggregate Map
Used by: agents/ddd-modeler.md, skills/ddd-modeling.md (the shape ddd-modeler fills when returning an
  aggregate map). Also referenced by docs/templates/module-overview.md for the split-module case.
Output: normally a report handed back — fold it into the target module's own doc (§ Internal Layering /
  § Notable Conventions) or the implementation plan; do NOT create a standalone per-module file for a
  single-project module (that would duplicate the module doc). Commit it as
  src/docs/architecture/modules/<ModuleName>/domain-model.md only for a split module that keeps one
  alongside its overview.md.
Do not populate this file itself — copy its structure into the output.
-->

# Domain Model: <ModuleName / scope>

## Aggregates

_Each aggregate: its root entity, the boundary (which entities/value objects are inside it), and what
it is in business terms. One consistency boundary = one aggregate = one transaction._

## Entities / Value Objects

| Type | Kind (entity / value object) | Aggregate | Responsibility (behavior it owns) |
|---|---|---|---|

## Relationships

_How aggregates relate — reference by id (never a hard link across a boundary), ownership, or an
eventual-consistency reaction. Flag any rule that needs another module's data: it goes through a
`Contracts` DI seam or a denormalized snapshot, never a reach into another module's domain._

## Invariants / Business Rules

_Rules enforced **on the aggregate/entity** (not in a handler/service). For a review: cite file:line
and whether it is actually enforced; rank unenforced/leaked invariants first._

## Domain Events

| Event | Raised by (aggregate) | Reason (cross-aggregate / cross-module reaction) | Handled by |
|---|---|---|---|

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
