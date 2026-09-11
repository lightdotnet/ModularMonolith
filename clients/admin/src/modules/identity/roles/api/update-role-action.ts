"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { getRoleById, updateRole } from "@/modules/identity/roles/api/roles.api";
import { dedupeClaims } from "@/lib/shared/dedupe-claims";
import type { RoleDto } from "@/modules/identity/roles/types/role";
import type { ClaimDto } from "@/types/claim";

export interface UpdateRoleFormState {
  error?: string;
  success?: boolean;
}

const PERMISSION_CLAIM_TYPE = "permission";

export async function updateRoleAction(
  _prevState: UpdateRoleFormState,
  formData: FormData,
): Promise<UpdateRoleFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();

  if (!id || !name) {
    return { error: "Name is required." };
  }

  const current = await getRoleById(id);
  if (!current.isSuccess || !current.data) {
    return { error: current.message || "Failed to load the current role." };
  }

  const otherClaims = parseOtherClaims(
    formData.get("otherClaims"),
    current.data.claims,
  );
  const permissionClaims = formData
    .getAll("permissions")
    .map((value) => ({ type: PERMISSION_CLAIM_TYPE, value: String(value) }));

  const role: RoleDto = {
    id,
    name,
    description: String(formData.get("description") ?? "") || undefined,
    claims: dedupeClaims([...otherClaims, ...permissionClaims]),
  };

  const result = await updateRole(role);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update role." };
  }

  revalidatePath("/identity/roles");
  return { success: true };
}

// The client reclassifies claims (e.g. a "permission" claim whose value
// isn't among the fetched permission definitions falls through to the Other
// claims section), so this may legitimately include "permission"-type
// claims too. `dedupeClaims` collapses any overlap with the checked
// permission checkboxes when the two lists are merged above.
function parseOtherClaims(
  raw: FormDataEntryValue | null,
  fallbackClaims: ClaimDto[],
): ClaimDto[] {
  if (typeof raw !== "string") return fallbackClaims;

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return fallbackClaims;

    return parsed.filter(
      (claim): claim is ClaimDto =>
        !!claim &&
        typeof claim.type === "string" &&
        typeof claim.value === "string",
    );
  } catch {
    return fallbackClaims;
  }
}
