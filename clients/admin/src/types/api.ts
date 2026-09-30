export type ResultCode =
  | "success"
  | "bad_request"
  | "unauthorized"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "error"
  | "unknown";

export interface ApiResponse {
  requestId: string;
  code: ResultCode;
  isSuccess: boolean;
  message: string;
}

export interface Result<T> extends ApiResponse {
  data: T;
}

export interface Paged<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  records: T[];
}

export interface PagedResult<T> extends ApiResponse {
  data: Paged<T>;
}
