import { organizationApi } from "@/lib/server/backend-api";

const { requestJson } = organizationApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CreateOrgUnitRequest,
  MoveOrgUnitRequest,
  OrgUnitDto,
  OrgUnitTreeNodeDto,
} from "@/modules/organization/departments/types/org-unit";
import type { EmployeeDto } from "@/modules/organization/employees";

export function getOrgUnitTree(companyId: string) {
  return guardCall(() =>
    requestJson<Result<OrgUnitTreeNodeDto[]>>(`org_unit/company/${companyId}/tree`),
  );
}

export function getOrgUnitById(id: string) {
  return guardCall(() => requestJson<Result<OrgUnitDto>>(`org_unit/${id}`));
}

export function getOrgUnitEmployees(id: string) {
  return guardCall(() => requestJson<Result<EmployeeDto[]>>(`org_unit/${id}/employee`));
}

export function getOrgUnitManagers(id: string) {
  return guardCall(() => requestJson<Result<EmployeeDto[]>>(`org_unit/${id}/manager`));
}

export function createOrgUnit(request: CreateOrgUnitRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("org_unit", { method: "POST", body: request }),
  );
}

export function updateOrgUnit(orgUnit: OrgUnitDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`org_unit/${orgUnit.id}`, { method: "PUT", body: orgUnit }),
  );
}

export function moveOrgUnit(id: string, request: MoveOrgUnitRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`org_unit/${id}/move`, { method: "PUT", body: request }),
  );
}

export function deleteOrgUnit(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`org_unit/${id}`, { method: "DELETE" }),
  );
}
