import type { ClaimDto } from "@/types/claim";

export function dedupeClaims(claims: ClaimDto[]): ClaimDto[] {
  const seen = new Set<string>();
  const result: ClaimDto[] = [];

  for (const claim of claims) {
    const key = `${claim.type}:${claim.value}`;
    if (seen.has(key)) continue;
    seen.add(key);
    result.push(claim);
  }

  return result;
}
