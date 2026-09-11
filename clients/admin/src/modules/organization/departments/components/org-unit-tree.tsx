"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  ChevronDown,
  ChevronRight,
  FolderTree,
  Pencil,
  Plus,
  ShieldCheck,
  Users2,
} from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty";
import { CreateOrgUnitDialog } from "@/modules/organization/departments/components/create-org-unit-dialog";
import { EditOrgUnitDialog } from "@/modules/organization/departments/components/edit-org-unit-dialog";
import { DeleteOrgUnitDialog } from "@/modules/organization/departments/components/delete-org-unit-dialog";
import { MoveOrgUnitDialog } from "@/modules/organization/departments/components/move-org-unit-dialog";
import { ViewOrgUnitManagersDialog } from "@/modules/organization/departments/components/view-org-unit-managers-dialog";
import { OrgUnitType, type OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

interface OrgUnitTreeProps {
  companyId: string;
  nodes: OrgUnitTreeNodeDto[];
  error?: string;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
}

type DialogAction = "create" | "edit" | "delete" | "move" | "managers";

export function OrgUnitTree({
  companyId,
  nodes,
  error,
  canCreate,
  canUpdate,
  canDelete,
}: OrgUnitTreeProps) {
  const router = useRouter();
  const [dialog, setDialog] = useState<DialogAction | null>(null);
  const [dialogKey, setDialogKey] = useState(0);
  const [selectedNode, setSelectedNode] = useState<OrgUnitTreeNodeDto | null>(null);
  const [newUnitParent, setNewUnitParent] = useState<OrgUnitTreeNodeDto | null>(null);
  const [newUnitType, setNewUnitType] = useState<OrgUnitType>(OrgUnitType.Department);

  function openCreate(parent: OrgUnitTreeNodeDto | null, type: OrgUnitType) {
    setNewUnitParent(parent);
    setNewUnitType(type);
    setDialogKey((key) => key + 1);
    setDialog("create");
  }

  function openEdit(node: OrgUnitTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("edit");
  }

  function openDelete(node: OrgUnitTreeNodeDto) {
    setSelectedNode(node);
    setDialog("delete");
  }

  function openMove(node: OrgUnitTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("move");
  }

  function openManagers(node: OrgUnitTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("managers");
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

      {canCreate && (
        <div>
          <Button size="sm" onClick={() => openCreate(null, OrgUnitType.Department)}>
            <Plus />
            Add department
          </Button>
        </div>
      )}

      {nodes.length === 0 ? (
        <Empty>
          <EmptyTitle>No departments yet</EmptyTitle>
          <EmptyDescription>Add a top-level department to get started.</EmptyDescription>
        </Empty>
      ) : (
        <div className="flex flex-col gap-1 rounded-md border border-border p-2">
          {nodes.map((node) => (
            <OrgUnitTreeNode
              key={node.id}
              node={node}
              depth={0}
              canCreate={canCreate}
              canUpdate={canUpdate}
              canDelete={canDelete}
              onAddChild={openCreate}
              onEdit={openEdit}
              onDelete={openDelete}
              onMove={openMove}
              onViewManagers={openManagers}
            />
          ))}
        </div>
      )}

      <CreateOrgUnitDialog
        key={`create-${dialogKey}`}
        open={dialog === "create"}
        onOpenChange={(open) => setDialog(open ? "create" : null)}
        companyId={companyId}
        parent={newUnitParent}
        type={newUnitType}
        onCreated={refresh}
      />
      <EditOrgUnitDialog
        key={`edit-${dialogKey}`}
        open={dialog === "edit"}
        onOpenChange={(open) => setDialog(open ? "edit" : null)}
        node={selectedNode}
        onUpdated={refresh}
      />
      <DeleteOrgUnitDialog
        open={dialog === "delete"}
        onOpenChange={(open) => setDialog(open ? "delete" : null)}
        node={selectedNode}
        onDeleted={refresh}
      />
      <MoveOrgUnitDialog
        key={`move-${dialogKey}`}
        open={dialog === "move"}
        onOpenChange={(open) => setDialog(open ? "move" : null)}
        companyId={companyId}
        node={selectedNode}
        onMoved={refresh}
      />
      <ViewOrgUnitManagersDialog
        key={`managers-${dialogKey}`}
        open={dialog === "managers"}
        onOpenChange={(open) => setDialog(open ? "managers" : null)}
        node={selectedNode}
      />
    </div>
  );
}

interface OrgUnitTreeNodeProps {
  node: OrgUnitTreeNodeDto;
  depth: number;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
  onAddChild: (parent: OrgUnitTreeNodeDto, type: OrgUnitType) => void;
  onEdit: (node: OrgUnitTreeNodeDto) => void;
  onDelete: (node: OrgUnitTreeNodeDto) => void;
  onMove: (node: OrgUnitTreeNodeDto) => void;
  onViewManagers: (node: OrgUnitTreeNodeDto) => void;
}

function OrgUnitTreeNode({
  node,
  depth,
  canCreate,
  canUpdate,
  canDelete,
  onAddChild,
  onEdit,
  onDelete,
  onMove,
  onViewManagers,
}: OrgUnitTreeNodeProps) {
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

        {node.type === OrgUnitType.Team ? (
          <Users2 className="size-4 shrink-0 text-muted-foreground" />
        ) : (
          <FolderTree className="size-4 shrink-0 text-muted-foreground" />
        )}

        <span className="font-medium">{node.name}</span>
        <span className="text-xs text-muted-foreground">{node.code}</span>
        <Badge variant="outline">{node.type}</Badge>
        {node.status !== "Active" && <Badge variant="outline">{node.status}</Badge>}

        <div className="ml-auto flex items-center gap-1">
          <Button
            aria-label="View managers"
            size="icon-xs"
            variant="outline"
            onClick={() => onViewManagers(node)}
          >
            <ShieldCheck />
          </Button>
          {(canCreate || canUpdate || canDelete) && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon-xs" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canCreate && node.type === OrgUnitType.Department && (
                  <DropdownMenuItem onClick={() => onAddChild(node, OrgUnitType.Department)}>
                    Add sub-department
                  </DropdownMenuItem>
                )}
                {canCreate && node.type === OrgUnitType.Department && (
                  <DropdownMenuItem onClick={() => onAddChild(node, OrgUnitType.Team)}>
                    Add team
                  </DropdownMenuItem>
                )}
                {canUpdate && (
                  <DropdownMenuItem onClick={() => onEdit(node)}>Edit</DropdownMenuItem>
                )}
                {canUpdate && (
                  <DropdownMenuItem onClick={() => onMove(node)}>Move</DropdownMenuItem>
                )}
                {canDelete && (
                  <DropdownMenuItem variant="destructive" onClick={() => onDelete(node)}>
                    Delete
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {hasChildren && expanded && (
        <div>
          {node.children.map((child) => (
            <OrgUnitTreeNode
              key={child.id}
              node={child}
              depth={depth + 1}
              canCreate={canCreate}
              canUpdate={canUpdate}
              canDelete={canDelete}
              onAddChild={onAddChild}
              onEdit={onEdit}
              onDelete={onDelete}
              onMove={onMove}
              onViewManagers={onViewManagers}
            />
          ))}
        </div>
      )}
    </div>
  );
}
