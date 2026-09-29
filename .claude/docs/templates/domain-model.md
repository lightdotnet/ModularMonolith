<!--
Template: Domain Model / Aggregate Map
Used by: agents/ddd-modeler.md, skills/ddd-modeling/SKILL.md (the shape ddd-modeler fills when returning a
  building-block design or an aggregate map).
Output: normally a report handed back and folded into the implementation plan. Commit it as
  src/docs/architecture/domain-model.md only when the user asks for the shared building blocks to be documented.
Do not populate this file itself — copy its structure into the output.
-->

# Domain Model: <scope>

## Aggregates / Building Blocks

_Each aggregate: its root entity, the boundary, and what it is in business terms (one consistency boundary = one
aggregate = one transaction). For shared building blocks: each base type and the guarantees it gives derived types._

## Entities / Value Objects

| Type | Kind (entity / value object / base type) | Aggregate or scope | Responsibility (behavior it owns) |
|---|---|---|---|

## Relationships

_How aggregates relate — reference by id, never a hard link across a boundary. A rule that needs another module's
data goes through a `<Module>.Contracts` seam or a denormalized snapshot._

## Invariants / Business Rules

_Rules enforced **on the aggregate/entity** (not in a handler/service). For a review: cite file:line and whether it
is actually enforced; rank unenforced/leaked invariants first._

## Domain Events

| Event | Raised by | Reason (cross-aggregate / cross-module reaction) | Handled by |
|---|---|---|---|

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
