/** Date-only values (expected delivery) are stored at midnight UTC and shown by their literal date. */
export function toDateInputValue(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : "";
}

/** dd/MM/yyyy, blank for null/empty. */
export function formatDateOnly(value: string | null | undefined): string {
  const text = toDateInputValue(value);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) return "";
  const [year, month, day] = text.split("-");
  return `${day}/${month}/${year}`;
}

/** `yyyy-MM-dd` input value -> ISO instant at midnight UTC, or undefined when blank/invalid. */
export function dateInputToIso(value: string): string | undefined {
  const text = value.trim();
  return /^\d{4}-\d{2}-\d{2}$/.test(text) ? `${text}T00:00:00.000Z` : undefined;
}
