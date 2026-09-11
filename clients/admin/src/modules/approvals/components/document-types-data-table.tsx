"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { FileText, Pencil, Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { CreateDocumentTypeDialog } from "@/modules/approvals/components/create-document-type-dialog";
import { EditDocumentTypeDialog } from "@/modules/approvals/components/edit-document-type-dialog";
import { DeleteDocumentTypeDialog } from "@/modules/approvals/components/delete-document-type-dialog";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

interface DocumentTypesDataTableProps {
  documentTypes: ApprovalDocumentTypeDto[];
  error?: DataTableErrorState;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
}

const baseColumns: DataTableColumn<ApprovalDocumentTypeDto>[] = [
  {
    id: "name",
    header: "Name",
    hideable: false,
    sortable: true,
    sortValue: (type) => type.name.toLowerCase(),
    cell: (type) => <span className="font-medium">{type.name}</span>,
  },
  { id: "code", header: "Code", cell: (type) => type.code },
  { id: "description", header: "Description", cell: (type) => type.description ?? "" },
  {
    id: "isActive",
    header: "Active",
    cell: (type) => (
      <Badge variant={type.isActive ? "default" : "outline"}>{type.isActive ? "Active" : "Inactive"}</Badge>
    ),
  },
];

export function DocumentTypesDataTable({
  documentTypes,
  error,
  canCreate,
  canUpdate,
  canDelete,
}: DocumentTypesDataTableProps) {
  const canRowActions = Boolean(canUpdate || canDelete);

  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selected, setSelected] = useState<ApprovalDocumentTypeDto | null>(null);

  function onRefresh() {
    startTransition(() => router.refresh());
  }

  const actions: DataTableAction[] | undefined = canCreate
    ? [
        {
          key: "create",
          label: "Add document type",
          icon: Plus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<ApprovalDocumentTypeDto>[] = canRowActions
    ? [
        ...baseColumns,
        {
          id: "actions",
          header: "",
          hideable: false,
          cell: (type) => (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canUpdate && (
                  <DropdownMenuItem
                    onClick={() => {
                      setSelected(type);
                      setEditDialogKey((key) => key + 1);
                      setEditOpen(true);
                    }}
                  >
                    Edit
                  </DropdownMenuItem>
                )}
                {canDelete && (
                  <DropdownMenuItem
                    onClick={() => {
                      setSelected(type);
                      setDeleteOpen(true);
                    }}
                    variant="destructive"
                  >
                    Delete
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          ),
        },
      ]
    : baseColumns;

  return (
    <>
      <DataTable
        columns={columns}
        data={documentTypes}
        rowKey={(type) => type.id}
        isLoading={isPending}
        actions={actions}
        onRefresh={onRefresh}
        pageNumber={1}
        pageSize={Math.max(documentTypes.length, 1)}
        totalPages={1}
        totalRecords={documentTypes.length}
        onPageChange={() => {}}
        error={error}
        emptyState={{
          icon: FileText,
          title: "No document types yet",
          description: "Add a document type (e.g. Leave request, Purchase order) to categorize approvals.",
        }}
      />
      <CreateDocumentTypeDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={onRefresh}
      />
      <EditDocumentTypeDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        documentType={selected}
        onUpdated={onRefresh}
      />
      <DeleteDocumentTypeDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        documentType={selected}
        onDeleted={onRefresh}
      />
    </>
  );
}
