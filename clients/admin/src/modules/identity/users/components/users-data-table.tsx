"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Pencil, Users as UsersIcon, UserPlus } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
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
import { CreateUserDialog } from "@/modules/identity/users/components/create-user-dialog";
import { DeleteUserDialog } from "@/modules/identity/users/components/delete-user-dialog";
import { EditUserDialog } from "@/modules/identity/users/components/edit-user-dialog";
import { UserStatusBadge } from "@/modules/identity/user-profile/components/user-status-badge";
import { getDisplayName, getInitials } from "@/lib/shared/user-display";
import type { UserDto } from "@/modules/identity/users/types/user";

interface UsersDataTableProps {
  records: UserDto[];
  searchValue: string;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
}

const baseColumns: DataTableColumn<UserDto>[] = [
  {
    id: "user",
    header: "User",
    hideable: false,
    cell: (user) => (
      <div className="flex items-center gap-2.5">
        <Avatar size="sm">
          <AvatarFallback>{getInitials(user)}</AvatarFallback>
        </Avatar>
        <div className="flex flex-col">
          <span className="font-medium">{getDisplayName(user)}</span>
          <span className="text-xs text-muted-foreground">@{user.userName}</span>
        </div>
      </div>
    ),
  },
  {
    id: "email",
    header: "Email",
    cell: (user) => user.email ?? "",
  },
  {
    id: "phone",
    header: "Phone",
    cell: (user) => user.phoneNumber ?? "",
  },
  {
    id: "status",
    header: "Status",
    cell: (user) => <UserStatusBadge status={user.status} />,
  },
  {
    id: "roles",
    header: "Roles",
    cell: (user) =>
      user.roles.length > 0 ? (
        <div className="flex flex-wrap gap-1">
          {user.roles.map((role) => (
            <Badge key={role} variant="outline">
              {role}
            </Badge>
          ))}
        </div>
      ) : (
        ""
      ),
  },
];

export function UsersDataTable({
  records,
  searchValue,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canCreate,
  canUpdate,
  canDelete,
}: UsersDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  // Bumped every time a dialog opens, forcing a fresh instance — useActionState's
  // error state has no direct reset, so remounting is what clears it.
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);

  function navigate(nextParams: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(nextParams)) {
      if (value) params.set(key, value);
      else params.delete(key);
    }

    startTransition(() => {
      router.push(`${pathname}?${params.toString()}`);
    });
  }

  const actions: DataTableAction[] | undefined = canCreate
    ? [
        {
          key: "create",
          label: "Create user",
          icon: UserPlus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<UserDto>[] =
    canUpdate || canDelete
      ? [
          ...baseColumns,
          {
            id: "actions",
            header: "",
            hideable: false,
            cell: (user) => (
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
                        setSelectedUser(user);
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
                        setSelectedUser(user);
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
        data={records}
        rowKey={(user) => user.id}
        isLoading={isPending}
        actions={actions}
        searchValue={searchValue}
        onSearchChange={(value) => navigate({ q: value || undefined, page: undefined })}
        searchPlaceholder="Search users..."
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: UsersIcon,
          title: "No users found",
          description: "Try adjusting your search.",
        }}
      />
      <CreateUserDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={() => router.refresh()}
      />
      <EditUserDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        user={selectedUser}
        onUpdated={() => router.refresh()}
      />
      <DeleteUserDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        user={selectedUser}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
