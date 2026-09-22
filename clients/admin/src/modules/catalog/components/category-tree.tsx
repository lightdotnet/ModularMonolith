"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ChevronDown, ChevronRight, Folder, Pencil, Plus } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty";
import { CreateCategoryDialog } from "@/modules/catalog/components/create-category-dialog";
import { EditCategoryDialog } from "@/modules/catalog/components/edit-category-dialog";
import { DeleteCategoryDialog } from "@/modules/catalog/components/delete-category-dialog";
import { MoveCategoryDialog } from "@/modules/catalog/components/move-category-dialog";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

interface CategoryTreeProps {
  nodes: CategoryTreeNodeDto[];
  error?: string;
  canManage?: boolean;
}

type DialogAction = "create" | "edit" | "delete" | "move";

export function CategoryTree({ nodes, error, canManage }: CategoryTreeProps) {
  const router = useRouter();
  const [dialog, setDialog] = useState<DialogAction | null>(null);
  const [dialogKey, setDialogKey] = useState(0);
  const [selectedNode, setSelectedNode] = useState<CategoryTreeNodeDto | null>(null);
  const [newCategoryParent, setNewCategoryParent] = useState<CategoryTreeNodeDto | null>(null);

  function openCreate(parent: CategoryTreeNodeDto | null) {
    setNewCategoryParent(parent);
    setDialogKey((key) => key + 1);
    setDialog("create");
  }

  function openEdit(node: CategoryTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("edit");
  }

  function openDelete(node: CategoryTreeNodeDto) {
    setSelectedNode(node);
    setDialog("delete");
  }

  function openMove(node: CategoryTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("move");
  }

  function refresh() {
    router.refresh();
  }

  return (
    <div className="flex flex-col gap-3">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {canManage && (
        <div>
          <Button size="sm" onClick={() => openCreate(null)}>
            <Plus />
            Add top-level category
          </Button>
        </div>
      )}

      {nodes.length === 0 ? (
        <Empty>
          <EmptyTitle>No categories yet</EmptyTitle>
          <EmptyDescription>Add a top-level category to get started.</EmptyDescription>
        </Empty>
      ) : (
        <div className="flex flex-col gap-1 rounded-md border border-border p-2">
          {nodes.map((node) => (
            <CategoryTreeNode
              key={node.id}
              node={node}
              depth={0}
              canManage={canManage}
              onAddChild={openCreate}
              onEdit={openEdit}
              onDelete={openDelete}
              onMove={openMove}
            />
          ))}
        </div>
      )}

      <CreateCategoryDialog
        key={`create-${dialogKey}`}
        open={dialog === "create"}
        onOpenChange={(open) => setDialog(open ? "create" : null)}
        parent={newCategoryParent}
        onCreated={refresh}
      />
      <EditCategoryDialog
        key={`edit-${dialogKey}`}
        open={dialog === "edit"}
        onOpenChange={(open) => setDialog(open ? "edit" : null)}
        node={selectedNode}
        onUpdated={refresh}
      />
      <DeleteCategoryDialog
        open={dialog === "delete"}
        onOpenChange={(open) => setDialog(open ? "delete" : null)}
        node={selectedNode}
        onDeleted={refresh}
      />
      <MoveCategoryDialog
        key={`move-${dialogKey}`}
        open={dialog === "move"}
        onOpenChange={(open) => setDialog(open ? "move" : null)}
        node={selectedNode}
        onMoved={refresh}
      />
    </div>
  );
}

interface CategoryTreeNodeProps {
  node: CategoryTreeNodeDto;
  depth: number;
  canManage?: boolean;
  onAddChild: (parent: CategoryTreeNodeDto) => void;
  onEdit: (node: CategoryTreeNodeDto) => void;
  onDelete: (node: CategoryTreeNodeDto) => void;
  onMove: (node: CategoryTreeNodeDto) => void;
}

function CategoryTreeNode({
  node,
  depth,
  canManage,
  onAddChild,
  onEdit,
  onDelete,
  onMove,
}: CategoryTreeNodeProps) {
  const [expanded, setExpanded] = useState(true);
  const hasChildren = node.children.length > 0;

  return (
    <div>
      <div
        className="flex items-center gap-1.5 rounded-md px-2 py-1.5 hover:bg-accent/50"
        style={{ paddingLeft: `${depth * 1.5 + 0.25}rem` }}
      >
        <button
          type="button"
          aria-label={expanded ? "Collapse" : "Expand"}
          onClick={() => setExpanded((value) => !value)}
          className="flex size-5 shrink-0 items-center justify-center text-muted-foreground"
          disabled={!hasChildren}
        >
          {hasChildren ? (
            expanded ? (
              <ChevronDown className="size-4" />
            ) : (
              <ChevronRight className="size-4" />
            )
          ) : null}
        </button>

        <Folder className="size-4 shrink-0 text-muted-foreground" />

        <span className="font-medium">{node.name}</span>

        <div className="ml-auto flex items-center gap-1">
          {canManage && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon-xs" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => onAddChild(node)}>
                  Add subcategory
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => onEdit(node)}>Edit</DropdownMenuItem>
                <DropdownMenuItem onClick={() => onMove(node)}>Move</DropdownMenuItem>
                <DropdownMenuItem variant="destructive" onClick={() => onDelete(node)}>
                  Delete
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {hasChildren && expanded && (
        <div>
          {node.children.map((child) => (
            <CategoryTreeNode
              key={child.id}
              node={child}
              depth={depth + 1}
              canManage={canManage}
              onAddChild={onAddChild}
              onEdit={onEdit}
              onDelete={onDelete}
              onMove={onMove}
            />
          ))}
        </div>
      )}
    </div>
  );
}
