/** Mirrors Location.Contracts/Common/LocationTypeStatus.cs */
export enum LocationTypeStatus {
  Active = "Active",
  Inactive = "Inactive",
}

/** Mirrors Location.Contracts/LocationTypes/LocationTypeDto.cs */
export interface LocationTypeDto {
  id: string;
  name: string;
  allowedParentTypeId?: string | null;
  canHaveChildren: boolean;
  status: LocationTypeStatus;
}

/** Mirrors Location.Contracts/LocationTypes/CreateLocationTypeRequest.cs */
export interface CreateLocationTypeRequest {
  /** User-defined ID — not auto-generated. */
  id: string;
  name: string;
  allowedParentTypeId?: string;
  canHaveChildren: boolean;
}

/** Mirrors Location.Contracts/LocationTypes/UpdateLocationTypeRequest.cs */
export interface UpdateLocationTypeRequest {
  name: string;
  allowedParentTypeId?: string;
  canHaveChildren: boolean;
  status: LocationTypeStatus;
}
