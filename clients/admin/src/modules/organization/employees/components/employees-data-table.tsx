"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Contact, Pencil, UserPlus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { CreateEmployeeDialog } from "@/modules/organization/employees/components/create-employee-dialog";
import { DeleteEmployeeDialog } from "@/modules/organization/employees/components/delete-employee-dialog";
import { EditEmployeeDialog } from "@/modules/organization/employees/components/edit-employee-dialog";
import { EmploymentStatus, type EmployeeDto } from "@/modules/organization/employees/types/employee";
import type { CompanyDto } from "@/modules/organization/companies/types/company";

interface EmployeesDataTableProps {
  companies: CompanyDto[];
  companyId: string;
  records: EmployeeDto[];
  searchValue: string;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
  canManageLogin?: boolean;
}

const baseColumns: DataTableColumn<EmployeeDto>[] = [
  {
    id: "employee",
    header: "Employee",
    hideable: false,
    cell: (employee) => (
      <div className="flex flex-col">
        <span className="font-medium">
          {employee.firstName} {employee.lastName}
        </span>
        <span className="text-xs text-muted-foreground">{employee.employeeCode}</span>
      </div>
    ),
  },
  { id: "email", header: "Email", cell: (employee) => employee.email ?? "" },
  { id: "phone", header: "Phone", cell: (employee) => employee.phoneNumber ?? "" },
  {
    id: "status",
    header: "Status",
    cell: (employee) => (
      <Badge variant={employee.employmentStatus === EmploymentStatus.Active ? "default" : "outline"}>
        {employee.employmentStatus}
      </Badge>
    ),
  },
  {
    id: "login",
    header: "Login",
    cell: (employee) => (employee.userId ? <Badge variant="outline">Linked</Badge> : ""),
  },
];

export function EmployeesDataTable({
  companies,
  companyId,
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
  canManageLogin,
}: EmployeesDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editDialogKey, setEditDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeDto | null>(null);

  // Pending filter state — only applied to the URL when "Search" is clicked,
  // mirroring NotificationsDataTable's customSearch pattern. Re-synced from
  // props if the URL changes externally (e.g. browser back/forward).
  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
  }

  const [pendingCompanyId, setPendingCompanyId] = useState(companyId);
  const [lastCompanyId, setLastCompanyId] = useState(companyId);
  if (companyId !== lastCompanyId) {
    setLastCompanyId(companyId);
    setPendingCompanyId(companyId);
  }

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
          label: "Create employee",
          icon: UserPlus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<EmployeeDto>[] =
    canUpdate || canDelete
      ? [
          ...baseColumns,
          {
            id: "actions",
            header: "",
            hideable: false,
            cell: (employee) => (
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
                        setSelectedEmployee(employee);
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
                        setSelectedEmployee(employee);
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

  const customSearch = (
    <>
      <Input
        className="w-56"
        aria-label="Search employees"
        placeholder="Search employees..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-56"
        aria-label="Filter by company"
        placeholder="All companies"
        value={pendingCompanyId}
        onChange={setPendingCompanyId}
        options={companies.map((company) => ({ value: company.id, label: company.name }))}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(employee) => employee.id}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch || undefined,
            companyId: pendingCompanyId || undefined,
            page: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: Contact,
          title: "No employees found",
          description: "Try adjusting your search or company filter.",
        }}
      />
      <CreateEmployeeDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        companies={companies}
        onCreated={() => router.refresh()}
      />
      <EditEmployeeDialog
        key={`edit-${editDialogKey}`}
        open={editOpen}
        onOpenChange={setEditOpen}
        employee={selectedEmployee}
        canManageLogin={canManageLogin}
        onUpdated={() => router.refresh()}
      />
      <DeleteEmployeeDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        employee={selectedEmployee}
        onDeleted={() => router.refresh()}
      />
    </>
  );
}
