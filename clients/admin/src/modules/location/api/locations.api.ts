import { locationApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CreateLocationRequest,
  LocationDto,
  LocationTreeNodeDto,
  MoveLocationRequest,
  UpdateLocationRequest,
} from "../types/location";

const { requestJson } = locationApi;

export function getLocationTree() {
  return guardCall(() => requestJson<Result<LocationTreeNodeDto[]>>("location/tree"));
}

export function getLocationById(id: string) {
  return guardCall(() => requestJson<Result<LocationDto>>(`location/${id}`));
}

export function getLocationChildren(id: string) {
  return guardCall(() => requestJson<Result<LocationTreeNodeDto[]>>(`location/${id}/children`));
}

export function createLocation(request: CreateLocationRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("location", { method: "POST", body: request }),
  );
}

export function updateLocation(id: string, request: UpdateLocationRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`location/${id}`, { method: "PUT", body: request }),
  );
}

export function moveLocation(id: string, request: MoveLocationRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`location/${id}/move`, { method: "PUT", body: request }),
  );
}

export function deleteLocation(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`location/${id}`, { method: "DELETE" }),
  );
}
