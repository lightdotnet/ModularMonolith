import { organizationApi } from "@/lib/server/backend-api";

const { requestJson } = organizationApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  AssignEmployeeOrgUnitRequest,
  CreateEmployeeLoginRequest,
  CreateEmployeeRequest,
  EmployeeDto,
  EmployeeSearchParams,
  LinkEmployeeLoginRequest,
  UpdateEmployeeMembershipRequest,
} from "@/modules/organization/employees/types/employee";

export function searchEmployees(params: EmployeeSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<EmployeeDto>>("employee/search", {
      method: "GET",
      query: {
        companyId: params.companyId,
        orgUnitId: params.orgUnitId,
        employmentStatus: params.employmentStatus,
        searchValue: params.searchValue,
        linkedToUserOnly:
          params.linkedToUserOnly === undefined ? undefined : String(params.linkedToUserOnly),
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getEmployeeById(id: string) {
  return guardCall(() => requestJson<Result<EmployeeDto>>(`employee/${id}`));
}

export function createEmployee(request: CreateEmployeeRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("employee", { method: "POST", body: request }),
  );
}

export function updateEmployee(employee: EmployeeDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employee.id}`, { method: "PUT", body: employee }),
  );
}

export function deleteEmployee(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${id}`, { method: "DELETE" }),
  );
}

export function assignEmployeeOrgUnit(employeeId: string, request: AssignEmployeeOrgUnitRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employeeId}/org_unit`, {
      method: "POST",
      body: request,
    }),
  );
}

export function updateEmployeeMembership(
  employeeId: string,
  orgUnitId: string,
  request: UpdateEmployeeMembershipRequest,
) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employeeId}/org_unit/${orgUnitId}`, {
      method: "PUT",
      body: request,
    }),
  );
}

export function removeEmployeeFromOrgUnit(employeeId: string, orgUnitId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employeeId}/org_unit/${orgUnitId}`, {
      method: "DELETE",
    }),
  );
}

export function createEmployeeLogin(employeeId: string, request: CreateEmployeeLoginRequest) {
  return guardCall(() =>
    requestJson<Result<string>>(`employee/${employeeId}/login`, {
      method: "POST",
      body: request,
    }),
  );
}

export function linkEmployeeLogin(employeeId: string, request: LinkEmployeeLoginRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employeeId}/login`, { method: "PUT", body: request }),
  );
}

export function unlinkEmployeeLogin(employeeId: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`employee/${employeeId}/login`, { method: "DELETE" }),
  );
}
