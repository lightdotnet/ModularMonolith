/** Mirrors the backend validators (Inventory.Contracts/Stock): unit cost <= 1,000,000,000 with at most 4 decimal places. */
export const MAX_UNIT_COST = 1_000_000_000;

/** Mirrors `QuantityDelta` bounds in RecordStockMovementRequestValidator. */
export const MAX_QUANTITY_DELTA = 1_000_000_000;

export type UnitCostParseResult = { value: number } | { error: string };

/**
 * Parses a raw unit-cost form value. Callers decide what an empty string means
 * (omitted vs. required) before calling; `allowZero: false` makes zero invalid.
 */
export function parseUnitCost(raw: string, options: { allowZero: boolean }): UnitCostParseResult {
  // Plain decimal notation only: rejects exponents and more than 4 decimal places.
  if (!/^-?\d+(\.\d+)?$/.test(raw) || !Number.isFinite(Number(raw))) {
    return { error: "Enter a valid number." };
  }
  if (!/^-?\d+(\.\d{1,4})?$/.test(raw)) {
    return { error: "Unit cost can have at most 4 decimal places." };
  }
  const value = Number(raw);
  if (value < 0) {
    return { error: options.allowZero ? "Unit cost must not be negative." : "Unit cost must be greater than zero." };
  }
  if (!options.allowZero && value === 0) {
    return { error: "Unit cost must be greater than zero." };
  }
  if (value > MAX_UNIT_COST) {
    return { error: "Unit cost must not exceed 1,000,000,000." };
  }
  return { value };
}
