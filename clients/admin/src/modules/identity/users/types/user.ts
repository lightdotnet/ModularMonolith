import type { ClaimDto } from "@/types/claim";

export interface UserDto {
  id: string;
  userName: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  status?: string | null;
  authProvider?: string | null;
  isDeleted: boolean;
  roles: string[];
  claims: ClaimDto[];
}

export interface CreateUserRequest {
  userName?: string;
  firstName?: string;
  lastName?: string;
  password?: string;
  email?: string;
  phoneNumber?: string;
  authProvider?: string;
}

export interface SearchUsersParams {
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface DomainUserDto {
  userName: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
}
