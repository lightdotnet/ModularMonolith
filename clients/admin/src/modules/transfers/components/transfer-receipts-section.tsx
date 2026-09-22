import { LocalDateTime } from "@/components/shared/local-date-time";
import { TransferReceiptStatusBadge } from "@/modules/transfers/components/transfer-status-badge";
import { TransferReceiptStatus, type StockTransferDto } from "@/modules/transfers/types/transfer";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

interface TransferReceiptsSectionProps {
  transfer: StockTransferDto;
}

/** Receipts recorded against the transfer; a voided receipt counts toward no received quantity and shows why. */
export function TransferReceiptsSection({ transfer }: TransferReceiptsSectionProps) {
  if (transfer.receipts.length === 0) {
    return <p className="text-sm text-muted-foreground">No receipts recorded.</p>;
  }

  const lineById = new Map(transfer.lines.map((line) => [String(line.id), line]));

  return (
    <ul className="flex flex-col gap-3">
      {transfer.receipts.map((receipt) => (
        <li key={receipt.id} className="flex flex-col gap-2 rounded-lg border border-border p-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span className="text-sm">
              <LocalDateTime value={receipt.receivedAt} />
            </span>
            <TransferReceiptStatusBadge status={receipt.status} />
          </div>

          <ul className="flex flex-col gap-1 text-sm">
            {receipt.lines.map((line) => {
              const transferLine = lineById.get(String(line.transferLineId));
              return (
                <li key={line.id} className="flex items-baseline justify-between gap-3">
                  <span className="min-w-0 truncate">
                    {transferLine ? `${transferLine.productName} (${transferLine.sku})` : ""}
                  </span>
                  <span className="shrink-0 font-medium">{formatQuantity(line.quantity)}</span>
                </li>
              );
            })}
          </ul>

          {receipt.status === TransferReceiptStatus.Voided && (
            <div className="rounded-md border bg-muted/50 p-2 text-sm">
              <p className="text-xs text-muted-foreground">
                Voided <LocalDateTime value={receipt.voidedAt} />
              </p>
              {receipt.voidReason && <p className="whitespace-pre-wrap break-words">{receipt.voidReason}</p>}
            </div>
          )}
        </li>
      ))}
    </ul>
  );
}
