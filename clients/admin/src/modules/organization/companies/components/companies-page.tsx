import { Card, CardContent } from "@/components/ui/card";
import { searchCompanies } from "@/modules/organization/companies/api/companies.api";
import { CompaniesDataTable } from "@/modules/organization/companies/components/companies-data-table";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { COMPANIES_PERMISSIONS } from "@/modules/organization/companies/constants/permissions";

const PAGE_SIZE = 10;

interface CompaniesPageProps {
  searchParams: Promise<{ q?: string; page?: string }>;
}

export async function CompaniesPage({ searchParams }: CompaniesPageProps) {
  const { session, denied } = await requirePermission(COMPANIES_PERMISSIONS.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, COMPANIES_PERMISSIONS.Create);
  const canUpdate = hasPermission(session, COMPANIES_PERMISSIONS.Update);
  const canDelete = hasPermission(session, COMPANIES_PERMISSIONS.Delete);

  const { q, page } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const result = await searchCompanies({
    searchValue: q,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load companies", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Companies</h1>
        <p className="text-sm text-muted-foreground">
          Manage the companies that own departments, teams, and employees.
        </p>
      </div>

      <Card>
        <CardContent>
          <CompaniesDataTable
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
          />
        </CardContent>
      </Card>
    </div>
  );
}
