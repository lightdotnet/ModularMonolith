"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Pencil, Plus, Tag } from "lucide-react";
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
import { CreateTypeDialog } from "@/modules/orders/components/create-type-dialog";
import { EditTypeDialog } from "@/modules/orders/components/edit-type-dialog";
import { DeleteTypeDialog } from "@/modules/orders/components/delete-type-dialog";
import { OrderTypeStatus, type OrderTypeDto } from "@/modules/orders/types/order-type";

interface OrderTypesPanelProps {
  orderTypes: OrderTypeDto[];
  error?: string;
  canManage?: boolean;
}

export function OrderTypesPanel({
  orderTypes,
  error,
  canManage,
}: OrderTypesPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedType, setSelectedType] = useState<OrderTypeDto | null>(null);

  const errorState: DataTableErrorState | undefined = error
    ? { title: "Unable to load types", description: error }
    : undefined;

  const actions: DataTableAction[] | undefined = canManage
    ? [
        {
          key: "create",
          label: "Add type",
          icon: Plus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const baseColumns: DataTableColumn<OrderTypeDto>[] = [
    {
      id: "id",
      header: "ID",
      hideable: false,
      cell: (t) => <code className="text-xs font-semibold">{t.id}</code>,
    },
    {
      id: "name",
      header: "Name",
      hideable: false,
      sortable: true,
      sortValue: (t) => t.name.toLowerCase(),
      cell: (t) => t.name,
    },
    {
      id: "category",
      header: "Category",
      cell: (t) => <Badge variant="outline">{t.category}</Badge>,
    },
    {
      id: "status",
      header: "Status",
      cell: (t) => (
        <Badge variant={t.status === OrderTypeStatus.Active ? "default" : "secondary"}>{t.status}</Badge>
      ),
    },
  ];

  const columns: DataTableColumn<OrderTypeDto>[] = canManage
    ? [
        ...baseColumns,
        {
          id: "actions",
          header: "",
          hideable: false,
          cell: (type) => {
            return (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button aria-label="Row actions" size="icon" variant="outline">
                    <Pencil />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    onClick={() => {
                      setSelectedType(type);
                      setEditDialogKey((key) => key + 1);
                      setEditOpen(true);
                    }}
                  >
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={() => {
                      setSelectedType(type);
                      setDeleteOpen(true);
                    }}
                    variant="destructive"
                  >
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            );
          },
        },
      ]
    : baseColumns;

  return (
    <>
      <DataTable
        columns={columns}
        data={orderTypes}
        rowKey={(t) => `${t.category}-${t.id}`}
        isLoading={isPending}
        actions={actions}
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={1}
        pageSize={Math.max(orderTypes.length, 1)}
        totalPages={1}
        totalRecords={orderTypes.length}
        onPageChange={() => {}}
        error={errorState}
        emptyState={{
          icon: Tag,
          title: "No types yet",
          description: "Add a fee or payment type to get started.",
        }}
      />
      <CreateTypeDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={() => router.refresh()}
      />
      <EditTypeDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        type={selectedType}
        onUpdated={() => router.refresh()}
      />
      <DeleteTypeDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        type={selectedType}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
