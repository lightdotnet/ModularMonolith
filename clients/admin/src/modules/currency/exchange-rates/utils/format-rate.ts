// Exchange rates need more precision than the app-wide #0,000.00 amount format: up to 8 fraction
// digits, trailing zeros trimmed (25,000 / 0.00004 / 1.0823).
const RATE_FORMAT = new Intl.NumberFormat("en-US", {
  minimumFractionDigits: 0,
  maximumFractionDigits: 8,
});

export function formatRate(rate: number | null | undefined): string {
  return rate === null || rate === undefined || !Number.isFinite(rate) ? "" : RATE_FORMAT.format(rate);
}
