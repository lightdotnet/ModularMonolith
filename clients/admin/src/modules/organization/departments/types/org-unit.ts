/** Mirrors `Organization.Contracts/OrgUnits/OrgUnitType.cs` — serialized by name. */
export enum OrgUnitType {
  Department = "Department",
  Team = "Team",
}

export interface OrgUnitDto {
  id: string;
  companyId: string;
  parentId?: string | null;
  type: OrgUnitType;
  name: string;
  code: string;
  managerEmployeeId?: string | null;
  description?: string | null;
  status: string;
}

export interface CreateOrgUnitRequest {
  companyId: string;
  parentId?: string;
  type: OrgUnitType;
  name: string;
  code: string;
  managerEmployeeId?: string;
  description?: string;
}

export interface MoveOrgUnitRequest {
  newParentId?: string;
}

export interface OrgUnitTreeNodeDto {
  id: string;
  parentId?: string | null;
  type: OrgUnitType;
  name: string;
  code: string;
  managerEmployeeId?: string | null;
  status: string;
  children: OrgUnitTreeNodeDto[];
}

/** Flattens a tree into a plain list — used to populate org-unit pickers (e.g. the "assign employee" dialog). */
export function flattenOrgUnitTree(nodes: OrgUnitTreeNodeDto[]): OrgUnitTreeNodeDto[] {
  const result: OrgUnitTreeNodeDto[] = [];
  for (const node of nodes) {
    result.push(node);
    if (node.children.length > 0) result.push(...flattenOrgUnitTree(node.children));
  }
  return result;
}
