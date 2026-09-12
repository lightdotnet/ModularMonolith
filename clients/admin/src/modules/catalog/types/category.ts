/** Mirrors Catalog.Contracts/Categories/CategoryDto.cs */
export interface CategoryDto {
  id: string;
  parentCategoryId?: string | null;
  name: string;
}

/** Mirrors Catalog.Contracts/Categories/CategoryTreeNodeDto.cs */
export interface CategoryTreeNodeDto {
  id: string;
  parentCategoryId?: string | null;
  name: string;
  children: CategoryTreeNodeDto[];
}

/** Mirrors Catalog.Contracts/Categories/CreateCategoryRequest.cs */
export interface CreateCategoryRequest {
  parentCategoryId?: string;
  name: string;
}

/** Mirrors Catalog.Contracts/Categories/UpdateCategoryRequest.cs */
export interface UpdateCategoryRequest {
  name: string;
}

/** Mirrors Catalog.Contracts/Categories/MoveCategoryRequest.cs */
export interface MoveCategoryRequest {
  newParentCategoryId?: string;
}

/** Flattens a tree into a plain list — used to populate category pickers. */
export function flattenCategoryTree(nodes: CategoryTreeNodeDto[]): CategoryTreeNodeDto[] {
  const result: CategoryTreeNodeDto[] = [];
  for (const node of nodes) {
    result.push(node);
    if (node.children.length > 0) result.push(...flattenCategoryTree(node.children));
  }
  return result;
}
