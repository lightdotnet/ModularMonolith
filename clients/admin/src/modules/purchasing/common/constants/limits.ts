/** Mirrors Purchasing.Contracts/Common/PurchasingLimits.cs. */
export const MAX_QUANTITY = 1_000_000;
export const MAX_AMOUNT = 1_000_000_000;
export const MAX_LINES = 200;
/** Amounts are stored with at most 4 decimals (PrecisionScale(19, 4)). */
export const MAX_AMOUNT_DECIMALS = 4;

/**
 * Session claim type carrying the current user's linked Organization employee id. Approvals and Leave requests
 * each keep an identical private constant that is not barrel-exported, so this domain keeps its own copy rather
 * than reaching into (or editing) another module.
 */
export const EMPLOYEE_ID_CLAIM_TYPE = "employee_id";
