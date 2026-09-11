export interface EmployeeLevelDto {
  id: string;
  companyId: string;
  name: string;
  code: string;
  rank: number;
  description?: string | null;
}

export interface CreateEmployeeLevelRequest {
  companyId: string;
  name: string;
  code: string;
  rank: number;
  description?: string;
}
