# Approval Module DDD Review

**Review date:** 2026-09-08

## Scope

Reviewed the `Approval.Api` and `Approval.Contracts` modules, plus the current `LeaveManagement` integration. This document focuses on correctness risks, bounded-context boundaries, and a DDD-oriented refactoring path.

## Findings

### Critical: Leave status is eventually repaired on reads, not driven by a reliable event

`ApprovalFinalizedEvent` and `ApprovalStepPendingEvent` are internal to `Approval.Api`. `LeaveManagement` therefore cannot subscribe to finalization, and reconciles a pending leave request only when it is queried. A workflow can be approved while its leave request remains `Pending` until a read happens.

This becomes a correctness and authorization problem: update and delete handlers authorize from the locally stale `LeaveRequest.Status`. They also ignore the result of `IApprovalService.CancelAsync`. A user can therefore attempt to edit or delete a request after its approval was finalized, and an update can create a replacement workflow after cancellation failed.

Relevant code:

- `src/LeaveManagement.Api/Application/LeaveRequests/LeaveRequestStatusSync.cs:9-13`
- `src/LeaveManagement.Api/Application/LeaveRequests/Commands/UpdateLeaveRequest.cs:65-102`
- `src/LeaveManagement.Api/Application/LeaveRequests/Commands/DeleteLeaveRequest.cs:33-41`

### Critical: The approval chain has no aggregate invariant

`ApprovalService.CreateAsync` sorts the supplied chain but does not validate its levels. Duplicate levels are persisted, then `DecideAsync` calls `SingleOrDefault` for the current level and throws an exception rather than returning a business error.

The service also accepts invalid or incomplete actor identifiers and has no canonical rule for whether levels must be consecutive.

Relevant code:

- `src/Approval.Api/Services/ApprovalService.cs:18-50`
- `src/Approval.Api/Services/ApprovalService.cs:87-96`

### High: Decisions are not concurrency-safe

There is no optimistic concurrency token on `ApprovalRequest`. Two concurrent decisions can both read `Pending`, both persist a transition, and both publish downstream notifications. The last writer determines the stored state, while external effects may have occurred twice.

Relevant code:

- `src/Approval.Api/Services/ApprovalService.cs:77-146`
- `src/Approval.Api/Data/ApprovalDbContext.cs:38-70`

### High: State persistence and publishing are not atomic

Approval state is saved before the notification event is published. A notification failure makes the API call fail even though the workflow has already been stored. Retrying can introduce duplicate workflows or repeated notifications.

Leave creation has a related distributed consistency issue: it commits a leave request, calls Approval through another context, and attempts a compensating delete on failure. It is not an atomic unit of work.

Relevant code:

- `src/Approval.Api/Services/ApprovalService.cs:53-67`
- `src/Approval.Api/Services/ApprovalService.cs:129-144`
- `src/LeaveManagement.Api/Application/LeaveRequests/Commands/CreateLeaveRequest.cs:54-87`

### Medium: Source references do not provide idempotency

`(RequestType, RequestId)` is intentionally non-unique and `GetByRequestAsync` returns the most recent row. This supports resubmission, but it also hides duplicate creation caused by retry after a timeout or a post-save failure.

Relevant code:

- `src/Approval.Api/Data/ApprovalDbContext.cs:42`
- `src/Approval.Api/Services/ApprovalService.cs:175-184`

### Low: Inactive document types can still be used

`ApprovalDocumentType.IsActive` filters catalog queries but `CreateAsync` only checks whether the referenced row exists. If inactive means unavailable for new workflows, that invariant is currently missing.

Relevant code:

- `src/Approval.Api/Services/ApprovalService.cs:21-26`
- `src/Approval.Api/Application/DocumentTypes/Queries/GetApprovalDocumentTypes.cs:20-25`

## Target Model

Keep Approval as an independent bounded context. Its aggregate root should own the workflow state machine.

```text
Leave aggregate
  -> ApprovalRequested integration message (outbox)
  -> Approval application service
  -> ApprovalWorkflow aggregate
  -> ApprovalCompleted integration message (outbox)
  -> Leave event handler updates LeaveRequest status
```

Suggested model:

- `ApprovalWorkflow` becomes the aggregate root, replacing the current anemic `ApprovalRequest` role.
- `ApprovalStep` is a private child entity exposed as a read-only collection.
- The aggregate exposes behavior: `Start`, `Decide`, and `Cancel`; public setters disappear.
- `ApprovalSubject` value object replaces the loose `RequestType` and `RequestId` pair.
- `SubmissionKey` or `Revision` makes retry and resubmission explicit and idempotent.
- Actor identifiers remain opaque external references, preserving the boundary with Identity and Organization.
- `ApprovalDocumentType` should either be a catalog aggregate or evolve into a versioned policy/template. It should not appear central to workflow behavior if it only acts as a tag.

`ApprovalWorkflow.Start` must validate a non-empty chain, unique positive levels, required approver identifiers, and the selected convention for consecutive levels. `Decide` must enforce pending state, current-step ownership, a rejection reason, and exactly one valid state transition.

## Refactoring Plan

1. Correctness first
   - Validate approval-chain invariants before persistence.
   - Require a successful `CancelAsync` result before mutating or deleting a linked leave request.
   - Reject inactive document types for new workflows if that is the intended business rule.
   - Add an optimistic concurrency token to the workflow and translate conflicts to a retryable response.

2. Move behavior into the aggregate
   - Introduce `ApprovalWorkflow` with private state and domain methods.
   - Add repository ports for workflow writes and read-model queries.
   - Let aggregate methods produce domain events; application handlers persist the aggregate and dispatch events.

3. Establish reliable cross-module integration
   - Publish contract-level integration events from `Approval.Contracts`, such as `ApprovalCompleted` and `ApprovalCancelled`.
   - Store outgoing events through a transactional outbox in the owning module.
   - Consume them idempotently through an inbox or processed-event record in LeaveManagement.
   - Remove the write-on-read `LeaveRequestStatusSync` mechanism after the event consumer is live.

4. Make submission semantics explicit
   - Replace generic lookup of the latest source row with a current-workflow reference owned by the source aggregate.
   - Include a submission key or revision in workflow creation.
   - Treat duplicate submissions as an idempotent success when the same key is used.

## Required Tests

- Reject duplicate, non-positive, and malformed approval levels.
- Verify two simultaneous decisions yield one transition and one published integration event.
- Verify a notification or outbox dispatcher failure does not create a duplicate workflow on retry.
- Verify an approved workflow prevents Leave update and delete even before any Leave read occurs.
- Verify a failed cancellation prevents Leave mutation or deletion.
- Verify consumers ignore a duplicate `ApprovalCompleted` event.

## Existing Strengths

The module already has a useful starting boundary: its own schema and DbContext, a dedicated contracts project, and no direct persistence access from LeaveManagement into Approval. The refactor should preserve those boundaries while moving business invariants into the aggregate and making cross-module effects reliable.
