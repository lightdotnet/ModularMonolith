import { identityApi } from "@/lib/server/backend-api";

const { requestJson } = identityApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type { CreateRoleRequest, RoleDto } from "@/modules/identity/roles/types/role";
import type { PermissionDefinition } from "@/modules/identity/roles/types/permission-definition";

export function getAllRoles() {
  return guardCall(() => requestJson<Result<RoleDto[]>>("role"));
}

export function getPermissions() {
  return guardCall(() =>
    requestJson<Result<PermissionDefinition[]>>("permissions"),
  );
}

export function getRoleById(id: string) {
  return guardCall(() => requestJson<Result<RoleDto>>(`role/${id}`));
}

export function createRole(request: CreateRoleRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("role", { method: "POST", body: request }),
  );
}

export function updateRole(role: RoleDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>("role", { method: "PUT", body: role }),
  );
}

export function deleteRole(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`role/${id}`, { method: "DELETE" }),
  );
}
