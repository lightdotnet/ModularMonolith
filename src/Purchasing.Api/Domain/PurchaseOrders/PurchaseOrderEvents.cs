using StarterKit.Shared.Entities;

namespace StarterKit.Purchasing.Api.Domain.PurchaseOrders;

internal sealed record PurchaseOrderSubmittedEvent(
    long PurchaseOrderId,
    long SupplierId,
    string ApprovalRequestId,
    DateTimeOffset SubmittedAt) : DomainEvent;

internal sealed record PurchaseOrderApprovedEvent(
    long PurchaseOrderId,
    long SupplierId,
    string LocationId,
    DateTimeOffset ApprovedAt) : DomainEvent;

internal sealed record PurchaseOrderRejectedEvent(
    long PurchaseOrderId,
    long SupplierId,
    DateTimeOffset RejectedAt) : DomainEvent;

internal sealed record PurchaseOrderReceivedEvent(
    long PurchaseOrderId,
    string LocationId,
    bool FullyReceived,
    DateTimeOffset ReceivedAt) : DomainEvent;

internal sealed record PurchaseOrderCancelledEvent(
    long PurchaseOrderId,
    long SupplierId,
    DateTimeOffset CancelledAt,
    string Reason) : DomainEvent;
