/** Mirrors Location.Contracts/Common/LocationStatus.cs */
export enum LocationStatus {
  Active = "Active",
  Inactive = "Inactive",
}

/** Mirrors Location.Contracts/Locations/LocationDto.cs */
export interface LocationDto {
  id: string;
  parentLocationId?: string | null;
  locationTypeId: string;
  name: string;
  code: string;
  status: LocationStatus;
}

/** Mirrors Location.Contracts/Locations/LocationTreeNodeDto.cs */
export interface LocationTreeNodeDto {
  id: string;
  parentLocationId?: string | null;
  locationTypeId: string;
  name: string;
  code: string;
  status: LocationStatus;
  children: LocationTreeNodeDto[];
}

/** Mirrors Location.Contracts/Locations/CreateLocationRequest.cs */
export interface CreateLocationRequest {
  parentLocationId?: string;
  locationTypeId: string;
  name: string;
  code: string;
}

/** Mirrors Location.Contracts/Locations/UpdateLocationRequest.cs */
export interface UpdateLocationRequest {
  name: string;
  code: string;
  status: LocationStatus;
}

/** Mirrors Location.Contracts/Locations/MoveLocationRequest.cs */
export interface MoveLocationRequest {
  newParentLocationId?: string;
}

/** Flattens a tree into a plain list — used to populate location pickers. */
export function flattenLocationTree(nodes: LocationTreeNodeDto[]): LocationTreeNodeDto[] {
  const result: LocationTreeNodeDto[] = [];
  for (const node of nodes) {
    result.push(node);
    if (node.children.length > 0) result.push(...flattenLocationTree(node.children));
  }
  return result;
}
