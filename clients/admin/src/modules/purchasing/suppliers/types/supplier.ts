import type { SupplierStatus } from "@/modules/purchasing/common";

/** Mirrors Purchasing.Contracts/Suppliers/SupplierDto.cs — `id` is a backend `long`, typed `string` like every other opaque id here. */
export interface SupplierDto {
  id: string;
  code: string;
  name: string;
  contactName?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  paymentTerms?: string | null;
  status: SupplierStatus;
}

/** Mirrors Purchasing.Contracts/Suppliers/CreateSupplierRequest.cs. */
export interface CreateSupplierRequest {
  code: string;
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  paymentTerms?: string;
}

/** Mirrors Purchasing.Contracts/Suppliers/UpdateSupplierRequest.cs. */
export type UpdateSupplierRequest = CreateSupplierRequest;

/** Mirrors Purchasing.Contracts/Suppliers/SearchSupplierRequest.cs. */
export interface SupplierSearchParams {
  status?: SupplierStatus;
  /** Matches the code or name (contains). */
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Field length limits mirrored from the backend validators. */
export const SUPPLIER_LIMITS = {
  code: 50,
  name: 200,
  contactName: 200,
  phone: 50,
  email: 256,
  address: 500,
  paymentTerms: 500,
} as const;
