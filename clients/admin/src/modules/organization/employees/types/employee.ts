import type { OrgUnitType } from "@/modules/organization/departments/types/org-unit";

/** Mirrors `Organization.Contracts/Employees/EmploymentStatus.cs` — serialized by name. */
export enum EmploymentStatus {
  Active = "Active",
  OnLeave = "OnLeave",
  Terminated = "Terminated",
}

/** Mirrors `Organization.Contracts/Employees/AssignmentType.cs` — serialized by name. */
export enum AssignmentType {
  Current = "Current",
  Acting = "Acting",
}

export interface EmployeeMembershipDto {
  orgUnitId: string;
  orgUnitName: string;
  orgUnitType: OrgUnitType;
  levelId?: string | null;
  levelName?: string | null;
  isPrimary: boolean;
  assignmentType: AssignmentType;
  isManager: boolean;
  startDate: string;
  endDate?: string | null;
}

export interface EmployeeDto {
  id: string;
  companyId: string;
  userId?: string | null;
  employeeCode: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  gender?: string | null;
  nationalId?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  address?: string | null;
  hireDate?: string | null;
  terminationDate?: string | null;
  employmentStatus: EmploymentStatus;
  avatarUrl?: string | null;
  memberships: EmployeeMembershipDto[];
}

export interface CreateEmployeeRequest {
  companyId: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  gender?: string;
  nationalId?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  hireDate?: string;
  avatarUrl?: string;
}

export interface EmployeeSearchParams {
  companyId?: string;
  orgUnitId?: string;
  employmentStatus?: EmploymentStatus;
  searchValue?: string;
  /** When true, only employees with a linked Identity user are returned. */
  linkedToUserOnly?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface AssignEmployeeOrgUnitRequest {
  orgUnitId: string;
  levelId?: string;
  isPrimary: boolean;
  assignmentType: AssignmentType;
  isManager: boolean;
}

export interface UpdateEmployeeMembershipRequest {
  levelId?: string;
  isPrimary: boolean;
  assignmentType: AssignmentType;
  isManager: boolean;
}

export interface CreateEmployeeLoginRequest {
  userName: string;
  password?: string;
  email?: string;
  phoneNumber?: string;
}

export interface LinkEmployeeLoginRequest {
  userId: string;
}
