import type { ClaimDto } from "@/types/claim";

export interface RoleDto {
  id: string;
  name: string;
  description?: string | null;
  claims: ClaimDto[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}
