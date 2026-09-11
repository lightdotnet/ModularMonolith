import { Card, CardContent } from "@/components/ui/card";
import { searchCompanies } from "@/modules/organization/companies/api/companies.api";
import { searchEmployees } from "@/modules/organization/employees/api/employees.api";
import { EmployeesDataTable } from "@/modules/organization/employees/components/employees-data-table";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { EMPLOYEES_PERMISSIONS } from "@/modules/organization/employees/constants/permissions";

const PAGE_SIZE = 10;
const COMPANY_PAGE_SIZE = 100;

interface EmployeesPageProps {
  searchParams: Promise<{ q?: string; page?: string; companyId?: string }>;
}

export async function EmployeesPage({ searchParams }: EmployeesPageProps) {
  const { session, denied } = await requirePermission(EMPLOYEES_PERMISSIONS.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, EMPLOYEES_PERMISSIONS.Create);
  const canUpdate = hasPermission(session, EMPLOYEES_PERMISSIONS.Update);
  const canDelete = hasPermission(session, EMPLOYEES_PERMISSIONS.Delete);
  const canManageLogin = hasPermission(session, EMPLOYEES_PERMISSIONS.ManageLogin);

  const { q, page, companyId } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const [companiesResult, result] = await Promise.all([
    searchCompanies({ pageSize: COMPANY_PAGE_SIZE }),
    searchEmployees({
      companyId: companyId || undefined,
      searchValue: q,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
  ]);

  const companies = companiesResult.data?.records ?? [];

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load employees", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Employees</h1>
        <p className="text-sm text-muted-foreground">
          Manage employee records, their department/team memberships, and login accounts.
        </p>
      </div>

      <Card>
        <CardContent>
          <EmployeesDataTable
            companies={companies}
            companyId={companyId ?? ""}
            records={paged?.records ?? []}
            searchValue={q ?? ""}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canCreate={canCreate}
            canUpdate={canUpdate}
            canDelete={canDelete}
            canManageLogin={canManageLogin}
          />
        </CardContent>
      </Card>
    </div>
  );
}
