import { Badge } from "@/components/ui/badge";
import { StockMovementReason } from "@/modules/inventory/types/stock";

const VARIANT_BY_REASON: Record<StockMovementReason, "default" | "outline" | "secondary"> = {
  [StockMovementReason.ManualAdjustment]: "default",
  [StockMovementReason.OrderPlacement]: "secondary",
  [StockMovementReason.OrderCancellationRestore]: "secondary",
  [StockMovementReason.PurchaseReceipt]: "outline",
  [StockMovementReason.TransferIn]: "outline",
  [StockMovementReason.TransferOut]: "outline",
  [StockMovementReason.PurchaseReturnOut]: "outline",
  // Quantity-neutral cost change, visually distinct from stock movements.
  [StockMovementReason.CostRevaluation]: "default",
};

interface StockMovementReasonBadgeProps {
  reason: StockMovementReason;
}

export function StockMovementReasonBadge({ reason }: StockMovementReasonBadgeProps) {
  return <Badge variant={VARIANT_BY_REASON[reason]}>{reason}</Badge>;
}
