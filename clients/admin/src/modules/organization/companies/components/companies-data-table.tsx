"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Building2, Pencil, Plus } from "lucide-react";
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
import { CreateCompanyDialog } from "@/modules/organization/companies/components/create-company-dialog";
import { DeleteCompanyDialog } from "@/modules/organization/companies/components/delete-company-dialog";
import { EditCompanyDialog } from "@/modules/organization/companies/components/edit-company-dialog";
import { OrganizationStatus, type CompanyDto } from "@/modules/organization/companies/types/company";

interface CompaniesDataTableProps {
  records: CompanyDto[];
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

const baseColumns: DataTableColumn<CompanyDto>[] = [
  {
    id: "name",
    header: "Name",
    hideable: false,
    cell: (company) => (
      <div className="flex flex-col">
        <span className="font-medium">{company.name}</span>
        <span className="text-xs text-muted-foreground">{company.code}</span>
      </div>
    ),
  },
  {
    id: "taxCode",
    header: "Tax code",
    cell: (company) => company.taxCode ?? "",
  },
  {
    id: "email",
    header: "Email",
    cell: (company) => company.email ?? "",
  },
  {
    id: "phone",
    header: "Phone",
    cell: (company) => company.phone ?? "",
  },
  {
    id: "status",
    header: "Status",
    cell: (company) => (
      <Badge variant={company.status === OrganizationStatus.Active ? "default" : "outline"}>
        {company.status}
      </Badge>
    ),
  },
];

export function CompaniesDataTable({
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
}: CompaniesDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedCompany, setSelectedCompany] = useState<CompanyDto | null>(null);

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
          label: "Create company",
          icon: Plus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<CompanyDto>[] =
    canUpdate || canDelete
      ? [
          ...baseColumns,
          {
            id: "actions",
            header: "",
            hideable: false,
            cell: (company) => (
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
                        setSelectedCompany(company);
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
                        setSelectedCompany(company);
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
        rowKey={(company) => company.id}
        isLoading={isPending}
        actions={actions}
        searchValue={searchValue}
        onSearchChange={(value) => navigate({ q: value || undefined, page: undefined })}
        searchPlaceholder="Search companies..."
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: Building2,
          title: "No companies found",
          description: "Try adjusting your search.",
        }}
      />
      <CreateCompanyDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={() => router.refresh()}
      />
      <EditCompanyDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        company={selectedCompany}
        onUpdated={() => router.refresh()}
      />
      <DeleteCompanyDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        company={selectedCompany}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
