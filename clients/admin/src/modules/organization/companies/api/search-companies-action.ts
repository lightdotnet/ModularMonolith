"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { searchCompanies } from "@/modules/organization/companies/api/companies.api";
import type { CompanyDto, SearchCompaniesParams } from "@/modules/organization/companies/types/company";
import type { Paged } from "@/types/api";

export interface SearchCompaniesState {
  data: Paged<CompanyDto> | null;
  error?: string;
}

/** Backs the company picker used by the Departments/Employees features — there is no unbounded "get all companies" endpoint, so callers pass a generously large `pageSize` instead. */
export async function searchCompaniesAction(
  params: SearchCompaniesParams,
): Promise<SearchCompaniesState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await searchCompanies(params);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load companies." };
  }

  return { data: result.data };
}
