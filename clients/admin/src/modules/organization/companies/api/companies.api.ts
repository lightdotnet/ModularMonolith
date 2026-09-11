import { organizationApi } from "@/lib/server/backend-api";

const { requestJson } = organizationApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  CompanyDto,
  CreateCompanyRequest,
  SearchCompaniesParams,
} from "@/modules/organization/companies/types/company";

export function searchCompanies(params: SearchCompaniesParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<CompanyDto>>("company", {
      method: "GET",
      query: {
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
        status: params.status,
      },
    }),
  );
}

export function getCompanyById(id: string) {
  return guardCall(() => requestJson<Result<CompanyDto>>(`company/${id}`));
}

export function createCompany(request: CreateCompanyRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("company", { method: "POST", body: request }),
  );
}

export function updateCompany(company: CompanyDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`company/${company.id}`, { method: "PUT", body: company }),
  );
}

export function deleteCompany(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`company/${id}`, { method: "DELETE" }),
  );
}
