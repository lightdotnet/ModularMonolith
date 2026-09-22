import {
  MAX_LINES,
  PurchaseReturnReason,
  isNumericId,
  isValidQuantity,
  parseEnumValue,
} from "@/modules/purchasing/common";
import {
  PURCHASE_RETURN_NOTE_MAX_LENGTH,
  type PurchaseReturnLineRequest,
} from "../types/purchase-return";

export interface ReturnPayloadInput {
  reason: string;
  note: string;
  lines: { goodsReceiptLineId: string; quantity: number; reason?: string }[];
}

/** Shared validation for create/update return actions (limits mirror the backend validators). Plain helper, not a Server Action. */
export function validateReturnPayload(
  input: ReturnPayloadInput,
):
  | { reason: PurchaseReturnReason; note?: string; lines: PurchaseReturnLineRequest[] }
  | { error: string } {
  const reason = parseEnumValue(PurchaseReturnReason, input.reason);
  if (!reason) return { error: "Select a return reason." };

  const note = input.note.trim();
  if (note.length > PURCHASE_RETURN_NOTE_MAX_LENGTH) {
    return { error: `Note must not exceed ${PURCHASE_RETURN_NOTE_MAX_LENGTH} characters.` };
  }

  if (input.lines.length === 0) return { error: "Select at least one line to return." };
  if (input.lines.length > MAX_LINES) return { error: `At most ${MAX_LINES} lines are allowed.` };

  const seen = new Set<string>();
  const lines: PurchaseReturnLineRequest[] = [];
  for (const line of input.lines) {
    if (!isNumericId(line.goodsReceiptLineId)) return { error: "Invalid receipt line." };
    if (seen.has(line.goodsReceiptLineId)) return { error: "A receipt line can appear only once per return." };
    seen.add(line.goodsReceiptLineId);
    if (!isValidQuantity(line.quantity)) {
      return { error: "Quantities must be whole numbers between 1 and 1,000,000." };
    }

    let lineReason: PurchaseReturnReason | undefined;
    if (line.reason) {
      lineReason = parseEnumValue(PurchaseReturnReason, line.reason);
      if (!lineReason) return { error: "Invalid line reason." };
    }
    lines.push({ goodsReceiptLineId: line.goodsReceiptLineId, quantity: line.quantity, reason: lineReason });
  }

  return { reason, note: note || undefined, lines };
}
