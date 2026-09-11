import { organizationApi } from "@/lib/server/backend-api";

const { requestJson } = organizationApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CreateEmployeeLevelRequest,
  EmployeeLevelDto,
} from "@/modules/organization/departments/types/employee-level";

export function getEmployeeLevels(companyId: string) {
  return guardCall(() =>
    requestJson<Result<EmployeeLevelDto[]>>(`employee_level/company/${companyId}`),
  );
}

export function createEmployeeLevel(request: CreateEmployeeLevelRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("employee_level", { method: "POST", body: request }),
  );
}

export function updateEmployeeLevel(level: EmployeeLevelDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee_level/${level.id}`, { method: "PUT", body: level }),
  );
}

export function deleteEmployeeLevel(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee_level/${id}`, { method: "DELETE" }),
  );
}
