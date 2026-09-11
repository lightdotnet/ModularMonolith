/** Mirrors `Organization.Contracts/Common/OrganizationStatus.cs` — the backend serializes this enum by name, not by number (same convention as `NotificationStatus`). */
export enum OrganizationStatus {
  Active = "Active",
  Inactive = "Inactive",
}

export interface CompanyDto {
  id: string;
  name: string;
  code: string;
  taxCode?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  description?: string | null;
  status: OrganizationStatus;
}

export interface CreateCompanyRequest {
  name: string;
  code: string;
  taxCode?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  description?: string;
}

export interface SearchCompaniesParams {
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
  status?: OrganizationStatus;
}
