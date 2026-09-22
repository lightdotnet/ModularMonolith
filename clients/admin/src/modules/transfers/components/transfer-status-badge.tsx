import { Badge } from "@/components/ui/badge";
import { TransferReceiptStatus, TransferStatus } from "@/modules/transfers/types/transfer";

const VARIANT_BY_STATUS: Record<TransferStatus, "default" | "outline" | "secondary" | "destructive"> = {
  [TransferStatus.Draft]: "outline",
  [TransferStatus.Posting]: "secondary",
  [TransferStatus.Dispatched]: "secondary",
  [TransferStatus.PartiallyReceived]: "secondary",
  [TransferStatus.Received]: "default",
  [TransferStatus.Closed]: "default",
  [TransferStatus.Cancelled]: "outline",
};

const LABEL_BY_STATUS: Record<TransferStatus, string> = {
  [TransferStatus.Draft]: "Draft",
  [TransferStatus.Posting]: "Processing",
  [TransferStatus.Dispatched]: "Dispatched",
  [TransferStatus.PartiallyReceived]: "Partially received",
  [TransferStatus.Received]: "Received",
  [TransferStatus.Closed]: "Closed",
  [TransferStatus.Cancelled]: "Cancelled",
};

export function formatTransferStatus(status: TransferStatus): string {
  return LABEL_BY_STATUS[status] ?? status;
}

interface TransferStatusBadgeProps {
  status: TransferStatus;
}

export function TransferStatusBadge({ status }: TransferStatusBadgeProps) {
  return <Badge variant={VARIANT_BY_STATUS[status] ?? "outline"}>{formatTransferStatus(status)}</Badge>;
}

const VARIANT_BY_RECEIPT_STATUS: Record<
  TransferReceiptStatus,
  "default" | "outline" | "secondary" | "destructive"
> = {
  [TransferReceiptStatus.Posting]: "secondary",
  [TransferReceiptStatus.Posted]: "default",
  [TransferReceiptStatus.Voided]: "destructive",
};

const LABEL_BY_RECEIPT_STATUS: Record<TransferReceiptStatus, string> = {
  [TransferReceiptStatus.Posting]: "Processing",
  [TransferReceiptStatus.Posted]: "Posted",
  [TransferReceiptStatus.Voided]: "Voided",
};

interface TransferReceiptStatusBadgeProps {
  status: TransferReceiptStatus;
}

export function TransferReceiptStatusBadge({ status }: TransferReceiptStatusBadgeProps) {
  return (
    <Badge variant={VARIANT_BY_RECEIPT_STATUS[status] ?? "outline"}>
      {LABEL_BY_RECEIPT_STATUS[status] ?? status}
    </Badge>
  );
}
