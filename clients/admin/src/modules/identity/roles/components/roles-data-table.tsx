"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { KeyRound, Pencil, ShieldPlus } from "lucide-react";
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
import { CreateRoleDialog } from "@/modules/identity/roles/components/create-role-dialog";
import { DeleteRoleDialog } from "@/modules/identity/roles/components/delete-role-dialog";
import { EditRoleDialog } from "@/modules/identity/roles/components/edit-role-dialog";
import type { RoleDto } from "@/modules/identity/roles/types/role";

interface RolesDataTableProps {
  roles: RoleDto[];
  error?: DataTableErrorState;
  canManage?: boolean;
}

const baseColumns: DataTableColumn<RoleDto>[] = [
  {
    id: "name",
    header: "Name",
    hideable: false,
    sortable: true,
    sortValue: (role) => role.name.toLowerCase(),
    cell: (role) => role.name,
  },
  {
    id: "description",
    header: "Description",
    sortable: true,
    sortValue: (role) => (role.description ?? "").toLowerCase(),
    cell: (role) => role.description ?? "",
  },
];

export function RolesDataTable({
  roles,
  error,
  canManage,
}: RolesDataTableProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [searchValue, setSearchValue] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<RoleDto | null>(null);

  const query = searchValue.trim().toLowerCase();
  const filteredRoles = query
    ? roles.filter(
        (role) =>
          role.name.toLowerCase().includes(query) ||
          (role.description ?? "").toLowerCase().includes(query),
      )
    : roles;

  const actions: DataTableAction[] | undefined = canManage
    ? [
        {
          key: "create",
          label: "Add role",
          icon: ShieldPlus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<RoleDto>[] = canManage
    ? [
        ...baseColumns,
        {
          id: "actions",
          header: "",
          hideable: false,
          cell: (role) => (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onClick={() => {
                    setSelectedRole(role);
                    setEditDialogKey((key) => key + 1);
                    setEditOpen(true);
                  }}
                >
                  Edit
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => {
                    setSelectedRole(role);
                    setDeleteOpen(true);
                  }}
                  variant="destructive"
                >
                  Delete
                </DropdownMenuItem>
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
        data={filteredRoles}
        rowKey={(role) => role.id}
        isLoading={isPending}
        actions={actions}
        searchValue={searchValue}
        onSearchChange={setSearchValue}
        searchPlaceholder="Search roles..."
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={1}
        pageSize={filteredRoles.length}
        totalPages={1}
        totalRecords={filteredRoles.length}
        onPageChange={() => {}}
        error={error}
        emptyState={{
          icon: KeyRound,
          title: "No roles found",
          description: "Try adjusting your search.",
        }}
      />
      <CreateRoleDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={() => router.refresh()}
      />
      <EditRoleDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        role={selectedRole}
        onUpdated={() => router.refresh()}
      />
      <DeleteRoleDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        role={selectedRole}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
