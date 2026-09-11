"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { GaugeCircle, Pencil, Plus } from "lucide-react";
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
import { CreateEmployeeLevelDialog } from "@/modules/organization/departments/components/create-employee-level-dialog";
import { EditEmployeeLevelDialog } from "@/modules/organization/departments/components/edit-employee-level-dialog";
import { DeleteEmployeeLevelDialog } from "@/modules/organization/departments/components/delete-employee-level-dialog";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

interface EmployeeLevelsPanelProps {
  companyId: string;
  levels: EmployeeLevelDto[];
  error?: string;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
}

const baseColumns: DataTableColumn<EmployeeLevelDto>[] = [
  {
    id: "name",
    header: "Name",
    hideable: false,
    sortable: true,
    sortValue: (level) => level.name.toLowerCase(),
    cell: (level) => level.name,
  },
  { id: "code", header: "Code", cell: (level) => level.code },
  {
    id: "rank",
    header: "Rank",
    sortable: true,
    sortValue: (level) => level.rank,
    cell: (level) => level.rank,
  },
  { id: "description", header: "Description", cell: (level) => level.description ?? "" },
];

export function EmployeeLevelsPanel({
  companyId,
  levels,
  error,
  canCreate,
  canUpdate,
  canDelete,
}: EmployeeLevelsPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedLevel, setSelectedLevel] = useState<EmployeeLevelDto | null>(null);

  const errorState: DataTableErrorState | undefined = error
    ? { title: "Unable to load employee levels", description: error }
    : undefined;

  const actions: DataTableAction[] | undefined = canCreate
    ? [
        {
          key: "create",
          label: "Add level",
          icon: Plus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<EmployeeLevelDto>[] =
    canUpdate || canDelete
      ? [
          ...baseColumns,
          {
            id: "actions",
            header: "",
            hideable: false,
            cell: (level) => (
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
                        setSelectedLevel(level);
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
                        setSelectedLevel(level);
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
        data={levels}
        rowKey={(level) => level.id}
        isLoading={isPending}
        actions={actions}
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={1}
        pageSize={Math.max(levels.length, 1)}
        totalPages={1}
        totalRecords={levels.length}
        onPageChange={() => {}}
        error={errorState}
        emptyState={{
          icon: GaugeCircle,
          title: "No levels yet",
          description: "Add a level (e.g. Junior, Senior, Lead) for this company.",
        }}
      />
      <CreateEmployeeLevelDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        companyId={companyId}
        onCreated={() => router.refresh()}
      />
      <EditEmployeeLevelDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        level={selectedLevel}
        onUpdated={() => router.refresh()}
      />
      <DeleteEmployeeLevelDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        level={selectedLevel}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
