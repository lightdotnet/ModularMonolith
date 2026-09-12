import { locationApi } from "@/lib/server/backend-api";
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, Result } from "@/types/api";
import type {
  CreateLocationTypeRequest,
  LocationTypeDto,
  UpdateLocationTypeRequest,
} from "../types/location-type";

const { requestJson } = locationApi;

export function getLocationTypes() {
  return guardCall(() => requestJson<Result<LocationTypeDto[]>>("location_type"));
}

export function getLocationTypeById(id: string) {
  return guardCall(() => requestJson<Result<LocationTypeDto>>(`location_type/${id}`));
}

export function createLocationType(request: CreateLocationTypeRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("location_type", { method: "POST", body: request }),
  );
}

export function updateLocationType(id: string, request: UpdateLocationTypeRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`location_type/${id}`, { method: "PUT", body: request }),
  );
}

export function deleteLocationType(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`location_type/${id}`, { method: "DELETE" }),
  );
}
