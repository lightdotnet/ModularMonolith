using StarterKit.Shared.Entities;

namespace StarterKit.Transfers.Api.Domain.StockTransfers;

internal sealed record TransferDispatchedEvent(
    long TransferId,
    string SourceLocationId,
    string DestinationLocationId,
    DateTimeOffset DispatchedAt) : DomainEvent;

internal sealed record TransferReceivedEvent(
    long TransferId,
    long ReceiptId,
    string DestinationLocationId,
    bool FullyReceived,
    DateTimeOffset ReceivedAt) : DomainEvent;

internal sealed record TransferClosedEvent(
    long TransferId,
    string SourceLocationId,
    string DestinationLocationId,
    DateTimeOffset ClosedAt,
    string Reason,
    decimal ValueLostBase) : DomainEvent;

internal sealed record TransferCancelledEvent(
    long TransferId,
    string SourceLocationId,
    string DestinationLocationId,
    DateTimeOffset CancelledAt,
    string Reason) : DomainEvent;
