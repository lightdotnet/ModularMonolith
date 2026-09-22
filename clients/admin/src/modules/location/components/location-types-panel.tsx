"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Layers, Pencil, Plus } from "lucide-react";
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
import { CreateLocationTypeDialog } from "@/modules/location/components/create-location-type-dialog";
import { EditLocationTypeDialog } from "@/modules/location/components/edit-location-type-dialog";
import { DeleteLocationTypeDialog } from "@/modules/location/components/delete-location-type-dialog";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

interface LocationTypesPanelProps {
  locationTypes: LocationTypeDto[];
  error?: string;
  canManage?: boolean;
}

const baseColumns: DataTableColumn<LocationTypeDto>[] = [
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
    id: "allowedParentTypeId",
    header: "Allowed parent",
    cell: (t) => t.allowedParentTypeId ?? <span className="text-muted-foreground">None</span>,
  },
  {
    id: "canHaveChildren",
    header: "Can have children",
    cell: (t) =>
      t.canHaveChildren ? (
        <Badge variant="default">Yes</Badge>
      ) : (
        <Badge variant="outline">No</Badge>
      ),
  },
  {
    id: "status",
    header: "Status",
    cell: (t) => (
      <Badge variant={t.status === "Active" ? "default" : "secondary"}>{t.status}</Badge>
    ),
  },
];

export function LocationTypesPanel({
  locationTypes,
  error,
  canManage,
}: LocationTypesPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedType, setSelectedType] = useState<LocationTypeDto | null>(null);

  const errorState: DataTableErrorState | undefined = error
    ? { title: "Unable to load location types", description: error }
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

  const columns: DataTableColumn<LocationTypeDto>[] = canManage
    ? [
        ...baseColumns,
        {
          id: "actions",
          header: "",
          hideable: false,
          cell: (locationType) => (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onClick={() => {
                    setSelectedType(locationType);
                    setEditDialogKey((key) => key + 1);
                    setEditOpen(true);
                  }}
                >
                  Edit
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => {
                    setSelectedType(locationType);
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
        data={locationTypes}
        rowKey={(t) => t.id}
        isLoading={isPending}
        actions={actions}
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={1}
        pageSize={Math.max(locationTypes.length, 1)}
        totalPages={1}
        totalRecords={locationTypes.length}
        onPageChange={() => {}}
        error={errorState}
        emptyState={{
          icon: Layers,
          title: "No location types yet",
          description: "Add a type (e.g. Country, Province, District) to get started.",
        }}
      />
      <CreateLocationTypeDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        existingTypes={locationTypes}
        onCreated={() => router.refresh()}
      />
      <EditLocationTypeDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        locationType={selectedType}
        existingTypes={locationTypes}
        onUpdated={() => router.refresh()}
      />
      <DeleteLocationTypeDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        locationType={selectedType}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
