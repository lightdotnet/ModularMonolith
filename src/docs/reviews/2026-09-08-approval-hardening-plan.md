# Approval + LeaveManagement Hardening — Implementation Plan

**Plan date:** 2026-09-08
**Status:** Draft — awaiting approval (CLAUDE.md §2.9, no code until approved)
**Scope:** `src/Approval.Api`, `src/Approval.Contracts`, `src/LeaveManagement.Api`, `src/Migrations/MSSQL/Approval`

## Origin

Consolidates findings from four reviews of the Approval module:

- `architecture-reviewer` — layering / module-boundary review (clean; anemic model flagged)
- `dotnet-architect` — bounded-context / module-shape review (context well-drawn; polling back-propagation weak)
- `ddd-modeler` — tactical DDD review (aggregate boundary correct; state machine in the service)
- Fact-check of `2026-09-08-approval-ddd-review.md` — all code-level claims verified accurate; net-new authorization finding surfaced

Then three planning passes (`ddd-modeler`, `dotnet-architect`, `efcore-specialist`) produced the phased design below.

## Findings addressed

| # | Finding | Severity | Phase |
|---|---|---|---|
| 1 | Anemic domain model — entire approval state machine lives in `ApprovalService` | High | 2 |
| 2 | Approval chain has no aggregate invariant — duplicate/non-positive levels persist; `DecideAsync` `SingleOrDefault` throws `InvalidOperationException` (500) instead of a `Result` error | High | 2 |
| 3 | Decisions are not concurrency-safe — no optimistic concurrency token on `ApprovalRequest` | High | 3 |
| 4 | Domain events hand-published from the service after `SaveChangesAsync`; `AddDomainEvent` / `DispatchDomainEvents` unused (the P6 twin, P6 documented for Identity only) | Medium | 2 + 3 |
| 5 | Steps left `Pending` forever after a request finalizes; `ApprovalStepStatus.Skipped` unreachable | Medium | 2 |
| 6 | `CancelAsync` has no "who may cancel" rule and publishes no event | Medium | 2 |
| 7 | No cross-module integration event — `LeaveManagement` reconciles approval status on every read (`LeaveRequestStatusSync`), an N+1 write-on-read from GET handlers | Medium-High | 1 + 4 |
| 8 | `LeaveManagement` update/delete authorize off stale local `LeaveRequest.Status`; `CancelAsync` `IResult` discarded → replacement workflow created after a failed cancel, original orphaned | High (net-new, untracked) | 5 |
| 9 | `CreateLeaveRequest` is not an atomic unit of work — commits the leave row, then calls Approval through a separate `DbContext`, best-effort compensating delete | Medium (tracked P8) | 6 |
| 10 | `ApprovalDeepLink` hardcodes the admin-client route `/approvals/requests/{id}` into the backend | Low-Medium | 3 |
| 11 | Inactive `ApprovalDocumentType` still usable for new workflows — `CreateAsync` checks row existence only, not `IsActive` | Low | 3 |
| 12 | `ApprovalRequestByIdSpec` applied inconsistently (read handlers use it, service inlines) | Low | 3 |
| 13 | `IApprovalService.GetByRequestAsync` returns the full `ApprovalRequestDto` when the only cross-module consumer needs `Status` | Low | 1 + 3 |

## Architecture decisions (locked)

| Question | Decision | Rationale |
|---|---|---|
| Rename `ApprovalRequest` → `ApprovalWorkflow` | **No** | Splits the ubiquitous language with `Approval.Contracts` (`ApprovalRequestDto`, `IApprovalService.GetByRequestAsync`) and `LeaveManagement`'s `entity.ApprovalRequestId` |
| `SourceReference` / `ApprovalSubject` value object for `(RequestType, RequestId)` | **No** | Repo uses a plain string for every opaque cross-module pointer (`Employee.UserId`, `Notification.FromUserId`); a VO forces Mapster/EF mapping work for zero functional gain. Factory guard + existing index cover it |
| `ApprovalRequestByIdSpec` | **Keep, use everywhere** including `ApprovalService` load paths | Repo-wide `*ByIdSpec` convention; the defect is the inconsistency |
| Module shape | **Stay single-project** (`.Api` + `.Contracts`, folder-organized) | Complexity is below the bar for a 4-way Clean-Architecture split; every sibling module is single-project; a split would disrupt `InternalsVisibleTo` test wiring |
| Transactional outbox / inbox / `SubmissionKey` / message broker | **No** (deferred) | One process, one physical database; `Lightsoft.EventBus` referenced but dead; the subscriber update is idempotent by construction, sufficient without a dedup store |
| Shared transaction across the two `DbContext`s | **No** — use ordering (workflow first, local commit last) | `TransactionScope` promotes to MSDTC across two connections (unavailable on the container targets); a shared connection breaks the one-DbContext-per-module seam |
| ADR location | **Inline subsection in `src/docs/architecture/architecture.md`** | `.claude/docs/ADR/` was tried and fully reverted 2026-08-13; do not recreate without a fresh request |
| Integration-event mechanism | **In-process `Light.Mediator.INotification`** declared in `Approval.Contracts`, published via `IPublisher`, handled by `INotificationHandler<T>` in `LeaveManagement.Api` | The established in-repo cross-module reaction pattern; one mediator spans all module assemblies |

## Chain-level convention (Finding 2)

Approval chain levels are required to be **positive and unique only** — **not** 1-based, **not** contiguous. Approval is a generic engine; the calling module owns chain resolution and may legitimately produce sparse levels. The advance logic already works with gaps ("next step with `Level > CurrentLevel`, ascending"). Levels are ordinal markers, not array indices.

## Who-may-cancel rule (Finding 6)

**Only the requester may cancel** (`cancelledByUserId == RequesterUserId`). Approvers *reject*; they do not cancel. There is no admin force-cancel endpoint today, so a single rule suffices. `LeaveManagement` passes the leave request's own `entity.UserId` (the owner withdrawing).

---

## Phase 1 — `Approval.Contracts` (purely additive)

| File | Change |
|---|---|
| `Approvals/ApprovalFinalizedIntegrationEvent.cs` | **New** — `public sealed record ApprovalFinalizedIntegrationEvent(string RequestType, string RequestId, string ApprovalRequestId, ApprovalStatus Status) : Light.Mediator.INotification;` |
| `Approvals/ApprovalStatusView.cs` | **New** — `public sealed record ApprovalStatusView(string ApprovalRequestId, string RequestType, string RequestId, ApprovalStatus Status, int CurrentLevel);` |
| `Services/IApprovalService.cs` | `CancelAsync` gains `string cancelledByUserId`; add `GetStatusByRequestAsync(string requestType, string requestId, CancellationToken)` and `GetStatusesByRequestAsync(string requestType, IReadOnlyCollection<string> requestIds, CancellationToken)` |
| `Approval.Contracts.csproj` | Add `<PackageReference Include="Lightsoft.Mediator" />` (version centrally pinned); optional `global using Light.Mediator;` in `GlobalUsings.cs` |

- **No** `ApprovalStepPendingIntegrationEvent` — `LeaveManagement` uses a single-level chain and only tracks `Pending` vs terminal; a mid-chain advance has no consumer. Documented as an extension point.
- `GetByRequestAsync` disposition — see **Open decisions**.

---

## Phase 2 — Domain aggregate redesign (`src/Approval.Api/Domain/Approvals/`)

### `ApprovalStep.cs` — rewrite

- All properties → `private set`; `private ApprovalStep()` ctor for EF.
- `internal static ApprovalStep Create(int level, string approverUserId, string approverEmployeeId, string? approverName)`
- `internal void Approve(string? comment, DateTimeOffset decidedAt)` / `Reject(...)` / `Skip()` (no-op unless `Pending`)
- `internal bool IsPending => Status == ApprovalStepStatus.Pending;`

### `ApprovalRequest.cs` — rewrite as aggregate root (name kept)

- All mutable properties → `{ get; private set; }`; remove `CurrentLevel = 1` and `Status = Pending` initializers (the factory always sets both); `private ApprovalRequest()` ctor.
- `private readonly List<ApprovalStep> _steps = [];` + `public IReadOnlyList<ApprovalStep> Steps => _steps;`
- `DocumentType` navigation stays `public` (read-only join for `.Adapt`).
- `ConcurrencyToken` property — added in Phase 3.

**`static IResult<ApprovalRequest> Create(...)`** — enforces, returning `Result<ApprovalRequest>.Error(...)`, never throwing:

| Invariant | Rule |
|---|---|
| non-empty chain | `approverChain.Count > 0` |
| positive levels | every `Level >= 1` |
| unique levels | no duplicate `Level` (fixes the `SingleOrDefault` 500) |
| approver ids present | every step: `ApproverUserId` and `ApproverEmployeeId` non-blank |
| requester present | `requesterUserId` non-blank |
| title present | `title` non-blank |

Sets `CurrentLevel = approverChain.Min(x => x.Level)`. Raises `ApprovalStepPendingEvent` for the first step via `AddDomainEvent`.

**`IResult Decide(string decidedByUserId, bool approved, string? comment, DateTimeOffset decidedAt)`**:

- Guards: `decidedByUserId` non-blank; `Status == Pending`; caller is the current step's assigned approver; a reason is required to reject.
- Resolve current step by `Level == CurrentLevel` (now guaranteed unique + present); keep a defensive `SingleOrDefault` → `Result.Error` guard, never an exception.
- Approve, not last level → `currentStep.Approve(...)`, advance `CurrentLevel` to the next step's level, raise `ApprovalStepPendingEvent` for it.
- Approve, last level → `currentStep.Approve(...)`, set `Status = Approved`, `FinalizedAt`, `Skip()` any remaining `Pending` step, raise `ApprovalFinalizedEvent`.
- Reject → `currentStep.Reject(...)`, set `Status = Rejected`, `FinalizedAt`, `Skip()` all downstream `Pending` steps, `CurrentLevel` unchanged, raise `ApprovalFinalizedEvent`.

**`IResult Cancel(string cancelledByUserId, DateTimeOffset cancelledAt)`**:

- Guards: `cancelledByUserId` non-blank; `Status == Pending`; `cancelledByUserId == RequesterUserId`.
- Set `Status = Cancelled`, `FinalizedAt`; `Skip()` all `Pending` steps.
- Raise `ApprovalRequestCancelledEvent` (new).

### `ApprovalRequestCancelledEvent.cs` — new

`internal sealed record ApprovalRequestCancelledEvent(...) : DomainEvent` in `Domain/Approvals/`. Payload: `Id`, `Title`, `DeepLinkUrl`, `RequesterUserId`, `CancelledByUserId`, current-step `ApproverUserId` (nullable).

### Domain events

`ApprovalStepPendingEvent` / `ApprovalFinalizedEvent` record shapes unchanged; they stay `internal sealed record … : DomainEvent`. The aggregate raises all three via `AddDomainEvent(...)`. No manual `IPublisher.Publish` anywhere in the domain.

### Factory input type

Reuse `StarterKit.Approval.Contracts.Approvals.ApproverStepInput` as the `Create` chain input (the domain layer already depends on `Approval.Contracts` for the enums; `ApproverStepInput` is a pure resolved-input record). Alternative if strict isolation is preferred: `Domain/Approvals/ApproverAssignment.cs` with the same four fields, mapped in the service — see **Open decisions**.

---

## Phase 3 — `ApprovalService` orchestrator + EF Core + integration event + smaller items

### `ApprovalService.cs` — thin orchestrator

- Drop `IPublisher` from the constructor (dispatch moves to the DbContext).
- `CreateAsync`: doc-type check becomes `AnyAsync(x => x.Id == request.DocumentTypeId && x.IsActive)` (Finding 11) with a distinct "not active" error → `ApprovalRequest.Create(...)` → map failure → `AddAsync` + `SaveChangesAsync` → `Result<string>.Success(entity.Id)`.
- `DecideAsync` / `CancelAsync`: load via `new ApprovalRequestByIdSpec(id)` + `.Include(x => x.Steps)` (Finding 12) → call the aggregate method → `SaveChangesAsync` → return the aggregate's `IResult` verbatim.
- **Concurrency (Finding 3):** wrap the `SaveChangesAsync` in `DecideAsync` / `CancelAsync` in `try/catch (DbUpdateConcurrencyException)`. On conflict: one bounded auto-retry — detach the stale entity, reload fresh with steps, re-run the guard chain (if the winner already finalized, the guards return the correct business error); after the retry, surface `Result.Conflict(...)`. Publish events only once, only on the successful path.
- **Integration event (Finding 7):** after a successful `SaveChangesAsync`, publish `ApprovalFinalizedIntegrationEvent` (terminal statuses only), **wrapped in `try/catch`**, logged at warning, swallowed — a subscriber fault must never fail a decide/cancel call. Published *after* the internal Notifications event, *outside* any transaction. `CancelAsync` publishes it too (`Status: Cancelled`) — today it emits nothing.
- **Lean projections (Finding 13):** `GetStatusByRequestAsync` and `GetStatusesByRequestAsync` as `AsNoTracking` hand-written `.Select(x => new ApprovalStatusView { ... })` — no `.Include`, no step projection. Batch: `WHERE RequestType == @t AND requestIds.Contains(RequestId)`, then in-memory `GroupBy(RequestId).Select(most-recent-by-Created)`, returned keyed by `RequestId`. Caller batch size capped (config, ~200).

### `ApprovalDbContext.cs`

- Inject `IPublisher` into the primary constructor. After `await base.SaveChangesAsync(...)` in the async override, call `await publisher.DispatchDomainEvents(this);` (fixes the P6 twin, Finding 4). Sync `SaveChanges` stays audit-only with a comment that the Approval write path is 100% async.
- **Concurrency token:** `entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken().HasMaxLength(32).IsRequired()` placed right after `ToTable` / `HasIndex` per the builder-order convention. **App-managed string token** (`Guid.NewGuid().ToString("N")`), rotated in a private helper called from both `SaveChanges` overrides for `ChangeTracker.Entries<ApprovalRequest>()` in state `Modified` — next to `AuditEntries(...)`. Uniform across all four providers (`rowversion`/`xmin` rejected — they force provider branching; Sqlite has no native support, which is the tie-breaker). Module-local, no shared-kernel change.
- `_steps` navigation → `SetPropertyAccessMode(PropertyAccessMode.Field)` in `ConfigureModel` so EF writes through the backing field; `.Include(x => x.Steps)` keeps working.
- Optional: widen the composite index to `HasIndex(x => new { x.RequestType, x.RequestId, x.Created })` to index-serve the `OrderByDescending(Created)` in the status projections (folds into the Phase 7 regeneration at no extra cost).

### `ApprovalService` — where `DbUpdateConcurrencyException` becomes a `Result`

Caught in the orchestrator, not the mediator handler. `DecideApprovalStepCommandHandler` is a pass-through and needs no change — `Result.Conflict` flows through `ApiControllerBase` unchanged. Verify the exact `Lightsoft.Result` factory name (`Result.Conflict(...)`) at implementation; fall back to `Result.Error` if the overload differs.

### `ApprovalDeepLink.cs` — delete (Finding 10)

Remove the file. `ApprovalStepPendingEventHandler` / `ApprovalFinalizedEventHandler` set `Url = notification.DeepLinkUrl` directly (`SystemMessage.Url` is `string?`; a null click target is valid). `CreateApprovalRequest.DeepLinkUrl` stays nullable (non-breaking). The only real consumer (`LeaveManagement`) always supplies one; the admin `POST approval` escape hatch "trusts the caller as-is". Flag for `api-contract-reviewer`: confirm the admin client's notification list tolerates a null link.

### `ApprovalRequestCancelledEventHandler.cs` — new

`INotificationHandler<ApprovalRequestCancelledEvent>` under `Application/Approvals/EventHandlers/` — notifies the current step's approver (if any) that the request was withdrawn; `FromUserId = CancelledByUserId`.

### `ApprovalService` constructor ripple

`IApprovalService.CancelAsync` gains `cancelledByUserId` → update `LeaveManagement`'s `UpdateLeaveRequest` and `DeleteLeaveRequest` call sites to pass `entity.UserId`.

---

## Phase 4 — `LeaveManagement`: consume the event, add the backstop, remove write-on-read (Finding 7)

| File | Change |
|---|---|
| `Application/LeaveRequests/LeaveRequestStatusSync.cs` | Strip to a pure function: `internal static LeaveRequestStatus MapStatus(ApprovalStatus? approvalStatus)`. Remove the `DbContext` / `IApprovalService` dependencies and `ReconcileAsync`. |
| `Application/LeaveRequests/EventHandlers/ApprovalFinalizedIntegrationEventHandler.cs` | **New** — `internal sealed class … : INotificationHandler<ApprovalFinalizedIntegrationEvent>`. Guard `RequestType == "LeaveRequest"`; PK lookup on `RequestId`; **stale-event guard `entity.ApprovalRequestId == notification.ApprovalRequestId`** (ignore a late event for a superseded workflow); `MapStatus`, persist only if changed. Whole body in `try/catch` — log, never rethrow. Idempotent by construction. Auto-registered by `AddMediatorFromAssemblies`. |
| `Application/LeaveRequests/LeaveRequestReconciliationService.cs` | **New `BackgroundService`** (the first in the backend). `PeriodicTimer` on `options.Interval`; `IServiceScopeFactory` (singleton must not capture the scoped `DbContext`); per tick load up to `options.BatchSize` (default 200) `LeaveRequests` where `Status == Pending && ApprovalRequestId != null`, oldest-`Created` first; one `GetStatusesByRequestAsync("LeaveRequest", ids)` call; `MapStatus`; persist changed rows in one `SaveChangesAsync`. Sweep body extracted to `ReconcileOnceAsync(...)` for unit testing. Config `LeaveManagement:Reconciliation` — `Enabled` (default `true`), `IntervalMinutes` (default 5). |
| `LeaveManagementModule.cs` | `services.AddHostedService<LeaveRequestReconciliationService>();` + `services.AddOptions<LeaveReconciliationOptions>().BindConfiguration("LeaveManagement:Reconciliation");` |
| `Application/LeaveRequests/Queries/SearchLeaveRequests.cs` | Delete the `pending` pre-query, the `LeaveRequestStatusSync.ReconcileAsync` call, and the `IApprovalService` constructor param. Pure read. |
| `Application/LeaveRequests/Queries/GetLeaveRequestById.cs` | Delete the `ReconcileAsync` call and the `IApprovalService` param. |

**Consequence:** a GET can briefly show `Pending` for a workflow that just finished if the in-process event lost the race or failed. Bounded by the reconcile interval; acceptable as display lag, and authorization no longer trusts this value (Phase 5).

---

## Phase 5 — `LeaveManagement`: authorization hole (Finding 8, net-new, untracked)

`Application/LeaveRequests/Commands/UpdateLeaveRequest.cs` and `DeleteLeaveRequest.cs`:

- **Reconcile-before-authorize.** After loading `entity`, if `entity.Status == Pending && entity.ApprovalRequestId != null`, call `approvalService.GetStatusByRequestAsync("LeaveRequest", entity.Id)` (single lean call). If the resolved status differs, write it to `entity.Status` and `SaveChangesAsync` **before** evaluating the ownership/status gate.
- **Check `CancelAsync` `IResult`.**
  - `UpdateLeaveRequest` — capture the result; if `!IsSuccess`, return the error and **do not** proceed to `CreateAsync` (prevents a replacement workflow + orphaned original).
  - `DeleteLeaveRequest` — capture the result; non-manage caller → abort with error on failure; manage caller (delete-any) → log and still delete the local row (manage delete is intentionally destructive), leaving the `ApprovalRequest` as a historical record. Document the asymmetry.
- `CreateAsync` result in `UpdateLeaveRequest` is already checked — leave as is.

---

## Phase 6 — `LeaveManagement`: atomicity via reordering (Finding 9)

`Application/LeaveRequests/Commands/CreateLeaveRequest.cs` — new sequence:

1. Validate (`EmployeeId` link, date range, approver candidate) — unchanged, before any write.
2. Resolve `requesterName` via `IOrgDirectoryService`.
3. Construct the `LeaveRequest` entity in memory — **do not** `AddAsync` / `SaveChanges` yet. (`entity.Id` is populated at construction by the vendor `AuditableEntity` ctor — **verify during implementation**.)
4. `approvalService.CreateAsync(... RequestId: entity.Id ...)`. On failure → return the error; nothing was persisted locally, no compensation needed.
5. On success → `entity.ApprovalRequestId = result.Data; entity.Status = Pending;` `AddAsync` + a **single** `SaveChangesAsync`.
6. If step 5 throws → best-effort `approvalService.CancelAsync(result.Data, ...)` in `try/catch`.

`UpdateLeaveRequest.cs` resubmit path — same ordering: cancel old → create new → only when the new `CreateAsync` succeeds, flip local `Status` / `ApprovalRequestId` and `SaveChanges`. Combined with the `CancelAsync` result check from Phase 5.

**Residual failure mode:** crash between the two calls orphans an `ApprovalRequest` (invisible to the leave UI, cleanable) instead of a `LeaveRequest` stuck `Pending` with no workflow (today, visible to the user forever) — strictly better.

---

## Phase 7 — Migration (regenerate, MSSQL only)

Per the dev migration squash rule: the module is still under active development → regenerate **one** `CreateApprovalSchema` baseline, **MSSQL only**. PostgreSQL and Sqlite baselines stay untouched (deliberate drift) until the module is finalized or on explicit request.

**Schema change:** one new column `ConcurrencyToken nvarchar(32) NOT NULL` on `approval.ApprovalRequests` (plus the widened index if adopted in Phase 3). `ApprovalStepStatus.Skipped` needs no DDL — the enum value already exists and `Status` is stored as `int`.

**Files regenerated:** `src/Migrations/MSSQL/Approval/{timestamp}_CreateApprovalSchema.cs` + `.Designer.cs` + `ApprovalDbContextModelSnapshot.cs`.

**Commands** (run by the user, from `src/Migrations/MSSQL`):

```bash
# delete the 3 existing MSSQL/Approval migration files, then:
dotnet ef migrations add CreateApprovalSchema --context ApprovalDbContext --output-dir Approval
dotnet ef migrations script --context ApprovalDbContext --idempotent   # inspect only
# dotnet ef database update --context ApprovalDbContext   → only on explicit go-ahead;
#   recreate the approval schema first (baseline keeps the same name, new contents)
```

**Risks:** an environment that applied the previous same-named baseline will not auto-pick-up the new column (history row already exists) — acceptable for a dev-only template repo, call it out in the PR. PG/Sqlite baselines drift from the model — deliberate per the squash rule; `PendingModelChangesWarning` is ignored in the migrator DI so the build is unaffected.

---

## Phase 8 — Tests *(separate explicit instruction required — CLAUDE.md §2.9)*

### New — `tests/Approval.Tests/Domain/Approvals/ApprovalRequestTests.cs` (pure aggregate, no DbContext)

Factory: empty chain, non-positive level, duplicate levels, blank approver user id, blank approver employee id, blank requester, blank title, sparse non-consecutive levels → `CurrentLevel == Min`, seeds `Pending` + all steps `Pending`, queues one `ApprovalStepPendingEvent` for the level-min approver.

Decide: blank `decidedByUserId`, caller not current approver, already finalized, reject without comment, approve-non-final advances + queues next pending event, approve-final sets `Approved` + `FinalizedAt` + queues finalized event, approve-final skips remaining pending steps, reject sets `Rejected` + marks current step + leaves `CurrentLevel` + queues finalized event, reject skips all downstream steps, duplicate-free levels never throw resolving the current step (regression for the 500).

Cancel: blank `cancelledByUserId`, caller not requester, not pending, requester cancels pending → `Cancelled` + `FinalizedAt` + skips pending steps + queues cancelled event.

### New — `tests/Approval.Tests/Data/ApprovalDbContextDispatchTests.cs`

`SaveChangesAsync` publishes queued domain events through the registered publisher and clears them; publishes nothing when none queued; the concurrency token blocks a double-decide (two contexts over one shared Sqlite connection → second `SaveChangesAsync` throws `DbUpdateConcurrencyException`).

### New — `tests/Approval.Tests/TestSupport/RecordingPublisher.cs`

Promote the `RecordingPublisher` currently private in `tests/Framework.Tests/Persistence/DispatchDomainEventsExtensionsTests.cs`; wire into `ApprovalTestHost` (`AddSingleton<IPublisher>`, exposed as a property).

### Rewrite — `tests/Approval.Tests/Services/ApprovalServiceTests.cs`

Constructor drops `IPublisher`; event assertions read `host.Publisher.Published`. Keep every existing scenario; add: reject when levels duplicated, reject when level non-positive (previously the untestable 500 path), reject when document type not active, `Conflict` / retry on concurrent decide, coverage for `GetStatusByRequestAsync` / `GetStatusesByRequestAsync`.

### `tests/LeaveManagement.Tests/`

- New `Application/LeaveRequests/EventHandlers/ApprovalFinalizedIntegrationEventHandlerTests.cs` — updates the row, idempotent on re-fire, ignores an event whose `ApprovalRequestId` no longer matches.
- New `ReconcileOnceAsync` unit test.
- Update `SearchLeaveRequestsQueryHandlerTests` / `GetLeaveRequestByIdQueryHandlerTests` — remove the `IApprovalService` mocks.
- Update `UpdateLeaveRequestCommandHandlerTests` / `DeleteLeaveRequestCommandHandlerTests` — approved-before-any-read blocks edit/delete; `CancelAsync` failure aborts (non-manage) / logs + deletes (manage); add the `cancelledByUserId` argument to mock `Setup` / `Verify`.
- New `CreateLeaveRequestCommandHandlerTests` fact — `CreateAsync` failure ⇒ no `LeaveRequest` row persisted.

---

## Phase 9 — Docs + ADR + known-debt *(separate explicit instruction required)*

- `src/docs/architecture/architecture.md` — new "Cross-Module Integration" subsection + the ADR inline (text drafted below); fix the Domain-events pattern bullet.
- `src/docs/architecture/modules/Approval.md` — Contracts inventory, the integration event, `Skipped` now reachable, `ApprovalDeepLink` removal, the `IsActive` rule, corrected route names (`approval/user`, `approval/document_type` — currently stale), the deferred step-event extension point.
- `src/docs/architecture/modules/LeaveManagement.md` — `LeaveRequestStatusSync` demoted to a mapper, the event handler, the `BackgroundService`, the auth-gate reconcile, the create/resubmit reorder.
- `src/docs/architecture/dependency-graph.md` — `Approval.Contracts → Lightsoft.Mediator`; the new cross-module mediator-notification edge.
- `src/docs/known-debt.md` — add Approval as the second P6 instance (then mark resolved); narrow/close P8; add the four net-new items (authorization window, missing concurrency token, chain invariant gap, inactive document type) and mark them resolved as each phase lands.
- Consider rewriting / superseding `src/docs/reviews/2026-09-08-approval-ddd-review.md` per its fact-check (demote the outbox recommendation to conditional, drop the `ApprovalWorkflow` rename and the "versioned policy" idea, note the single-physical-DB fact, cross-link known-debt).

### ADR draft — for `architecture.md`

> **ADR: In-process synchronous integration events now; transactional outbox only if a module is physically extracted**
>
> **Status:** Accepted — 2026-09-08
>
> **Context.** The backend is a Modular Monolith: one process, one physical database (all five module `DbContext`s alias `DefaultConnection`, separated only by schema). No message broker, no transactional outbox, no inbox/dedup table. `Lightsoft.EventBus` / `Lightsoft.EventBus.MassTransit.RabbitMQ` are referenced but have zero source usage — not adopted. The established cross-module reaction mechanism is a `Light.Mediator.INotification` published via `IPublisher` and handled by an `INotificationHandler<T>`; `AddMediatorFromAssemblies` registers handlers from every module assembly into one mediator, so a notification declared in `<ModuleA>.Contracts` can be handled inside `<ModuleB>.Api`. `IPublisher.Publish` is awaited in-line in the caller's DI scope; a handler exception propagates back into the publishing code path. Until now Approval's finalize/step events were `internal` to `Approval.Api`, so `LeaveManagement` reconciled leave status against `IApprovalService` on every read — an N+1 write-on-read.
>
> **Decision.**
> 1. Approval publishes a slim `ApprovalFinalizedIntegrationEvent` from `Approval.Contracts`, via `IPublisher`, immediately after the approval state is committed and outside any transaction. Approval's decision is the source of truth and is never rolled back by a downstream reaction.
> 2. `LeaveManagement` subscribes in-process with an `INotificationHandler<T>` and updates its own row. The handler catches and logs its own failures and never rethrows into the publisher; Approval also wraps the integration-event publish in `try/catch`. Delivery is best-effort-immediate.
> 3. A periodic in-module `BackgroundService` (`LeaveRequestReconciliationService`) is the guaranteed backstop for a missed or failed in-process delivery. It replaces the write-on-read reconcile.
> 4. No transactional outbox, inbox/dedup table, or broker. The subscriber update is idempotent by construction (a terminal status set from Approval's terminal status; re-applying is a no-op), which is sufficient without a dedup store.
> 5. Cross-module create/cancel consistency is handled by ordering (create the workflow first, commit the local row last) plus best-effort compensation, not a shared transaction. `TransactionScope` across the two `DbContext`s promotes to MSDTC, unavailable on the container targets.
>
> **Consequences.** Approval → LeaveManagement is eventually consistent, bounded by the backstop interval. A leave request briefly showing `Pending` after its workflow finished is display lag; authorization gates reconcile on demand before mutating. `<Module>.Contracts` projects may now carry `Light.Mediator.INotification` types and declare `Lightsoft.Mediator` directly. If a module is later physically extracted (the same intent that keeps the `.Api` suffixes, documented for `Identity.Api`), this decision is revisited: cross-process delivery then needs a producer-side outbox and a consumer-side inbox/dedup. Known-debt P7 and P8 are the markers for that escalation; this ADR is the recorded "why not yet".

---

## Execution order & blast radius

```
Phase 1 (Contracts, additive)
  → Phase 2 (Domain aggregate)
    → Phase 3 (ApprovalService + EF + events + smaller items)
      → Phase 4 (LeaveManagement consume + backstop)
        → Phase 5 (LeaveManagement authorization)
          → Phase 6 (LeaveManagement atomicity)
            → Phase 7 (MSSQL migration regeneration)
              → [STOP — await explicit instruction]
                → Phase 8 (tests)
                  → [STOP — await explicit instruction]
                    → Phase 9 (docs + ADR + known-debt)
```

- Each phase is independently revertible. Phase 1 is purely additive. Phases 2–6 are localized to one `.Api` project each (Phase 5 and 6 share two files — do them on one branch).
- Phase 4's `BackgroundService` is the highest-novelty item — mitigated by the `Enabled` config flag (default on), `IServiceScopeFactory`, batch cap, oldest-first ordering, all-caught exceptions per tick. Rollback = flip `Enabled=false` or remove the `AddHostedService` line; the event handler alone still covers the common path.
- Phase 4 removing read-path reconcile introduces a visible eventual-consistency window on GET. If unacceptable, a middle position is to keep a single **batched** `GetStatusesByRequestAsync` call in `SearchLeaveRequests` only (not the N+1, not in GetById).
- `Contracts` changes (Phase 1): adding `Lightsoft.Mediator` to a `Contracts` project is a new package edge — consistent with the "declare what you use" convention, version centrally pinned. Removing `GetByRequestAsync` is a seam narrowing with no `src/` consumer outside `LeaveManagement.Api` (verified); the admin client consumes Approval over HTTP only.

---

## Open decisions (need confirmation before implementation)

1. **`IApprovalService.GetByRequestAsync`** — remove it in Phase 1 (zero callers remain after the refactor) or keep it as a deprecated general-purpose lookup? *Recommendation: remove.*
2. **`CreateApprovalRequest.DeepLinkUrl`** — keep nullable (recommended, non-breaking) or make it required (breaking `Contracts` change, needs admin-client confirmation)?
3. **Who-may-cancel** — only the requester (recommended). Acceptable? The practical bite today is small (no HTTP cancel endpoint).
4. **Background reconcile interval** — 5 minutes proposed. Acceptable?
5. **`ApproverStepInput`** — reuse the `Approval.Contracts` record as the domain factory input (recommended, fewer files) or introduce a `Domain/Approvals/ApproverAssignment.cs` (strict isolation)?
6. **Full shared-transaction path** (touches all five modules' DbContext wiring) — defer entirely (recommended) or schedule as a follow-up ADR item now?

---

## Files touched (consolidated)

### `src/Approval.Contracts/`
- `Approvals/ApprovalFinalizedIntegrationEvent.cs` — new
- `Approvals/ApprovalStatusView.cs` — new
- `Services/IApprovalService.cs` — `CancelAsync` signature, +2 status methods, possibly −`GetByRequestAsync`
- `Approval.Contracts.csproj` — `Lightsoft.Mediator`
- `GlobalUsings.cs` — optional `global using Light.Mediator;`

### `src/Approval.Api/`
- `Domain/Approvals/ApprovalRequest.cs` — rewrite (encapsulation, factory, `Decide`, `Cancel`, events, `ConcurrencyToken`)
- `Domain/Approvals/ApprovalStep.cs` — rewrite (encapsulation, `internal` behavior methods)
- `Domain/Approvals/ApprovalRequestCancelledEvent.cs` — new
- `Domain/Approvals/ApprovalStepPendingEvent.cs` / `ApprovalFinalizedEvent.cs` — unchanged shape, raise sites move
- `Data/ApprovalDbContext.cs` — inject `IPublisher`, dispatch after save, concurrency-token config + rotation, `_steps` field access mode, optional wider index
- `Services/ApprovalService.cs` — thin orchestrator, `IsActive` check, `ApprovalRequestByIdSpec` load, concurrency catch + retry, integration-event publish, `GetStatusByRequestAsync` / `GetStatusesByRequestAsync`
- `Application/Approvals/EventHandlers/ApprovalDeepLink.cs` — delete
- `Application/Approvals/EventHandlers/ApprovalStepPendingEventHandler.cs` / `ApprovalFinalizedEventHandler.cs` — use `notification.DeepLinkUrl` directly
- `Application/Approvals/EventHandlers/ApprovalRequestCancelledEventHandler.cs` — new

### `src/LeaveManagement.Api/`
- `Application/LeaveRequests/LeaveRequestStatusSync.cs` — strip to `MapStatus`
- `Application/LeaveRequests/EventHandlers/ApprovalFinalizedIntegrationEventHandler.cs` — new
- `Application/LeaveRequests/LeaveRequestReconciliationService.cs` — new `BackgroundService`
- `LeaveManagementModule.cs` — `AddHostedService` + options binding
- `Application/LeaveRequests/Queries/SearchLeaveRequests.cs` / `GetLeaveRequestById.cs` — remove reconcile + `IApprovalService`
- `Application/LeaveRequests/Commands/CreateLeaveRequest.cs` — reorder (workflow first, single commit)
- `Application/LeaveRequests/Commands/UpdateLeaveRequest.cs` — reconcile-before-authorize, check `CancelAsync` result, reorder resubmit, pass `cancelledByUserId`
- `Application/LeaveRequests/Commands/DeleteLeaveRequest.cs` — reconcile-before-authorize, check `CancelAsync` result, pass `cancelledByUserId`

### `src/Migrations/MSSQL/Approval/`
- `{timestamp}_CreateApprovalSchema.cs` + `.Designer.cs` + `ApprovalDbContextModelSnapshot.cs` — regenerated (MSSQL only)

### Docs (Phase 9 only)
- `src/docs/architecture/architecture.md`, `src/docs/architecture/modules/Approval.md`, `src/docs/architecture/modules/LeaveManagement.md`, `src/docs/architecture/dependency-graph.md`, `src/docs/known-debt.md`, `src/docs/reviews/2026-09-08-approval-ddd-review.md`
