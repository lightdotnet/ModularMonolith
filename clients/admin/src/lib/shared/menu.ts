import type { NavItem } from "@/types/nav";

/**
 * Drops nodes whose `permission` the caller lacks, and drops groups left with
 * no visible children — never renders a group heading with nothing under it.
 */
export function buildVisibleMenu(
  items: NavItem[],
  can: (permission: string) => boolean,
): NavItem[] {
  return items.reduce<NavItem[]>((visible, item) => {
    if (item.permission && !can(item.permission)) {
      return visible;
    }

    if (!item.children) {
      visible.push(item);
      return visible;
    }

    const children = buildVisibleMenu(item.children, can);

    if (children.length === 0) {
      return visible;
    }

    visible.push({ ...item, children });
    return visible;
  }, []);
}

export interface NavLeaf {
  item: NavItem;
  /** Label of the top-level ancestor this leaf sits under (itself, if top-level). */
  section: string;
}

/**
 * Walks a (typically already permission-filtered) menu tree and returns its
 * navigable leaves — items with an `href` and no `children` — each tagged with
 * its top-level section label for grouping (e.g. in the command palette).
 */
export function flattenNavLeaves(items: NavItem[]): NavLeaf[] {
  const leaves: NavLeaf[] = [];

  const walk = (nodes: NavItem[], section: string) => {
    for (const node of nodes) {
      if (node.children && node.children.length > 0) {
        walk(node.children, section);
      } else {
        leaves.push({ item: node, section });
      }
    }
  };

  for (const top of items) {
    if (top.children && top.children.length > 0) {
      walk(top.children, top.label);
    } else {
      leaves.push({ item: top, section: top.label });
    }
  }

  return leaves;
}
