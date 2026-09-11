import { Card, CardContent } from "@/components/ui/card";
import { searchCompanies } from "@/modules/organization/companies/api/companies.api";
import { getOrgUnitTree } from "@/modules/organization/departments/api/org-units.api";
import { getEmployeeLevels } from "@/modules/organization/departments/api/employee-levels.api";
import { CompanyFilter } from "@/modules/organization/departments/components/company-filter";
import { OrgUnitTree } from "@/modules/organization/departments/components/org-unit-tree";
import { EmployeeLevelsPanel } from "@/modules/organization/departments/components/employee-levels-panel";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  EMPLOYEE_LEVELS_PERMISSIONS,
  ORG_UNITS_PERMISSIONS,
} from "@/modules/organization/departments/constants/permissions";

const COMPANY_PAGE_SIZE = 100;

interface DepartmentsPageProps {
  searchParams: Promise<{ companyId?: string }>;
}

export async function DepartmentsPage({ searchParams }: DepartmentsPageProps) {
  const { session, denied } = await requirePermission(ORG_UNITS_PERMISSIONS.View);
  if (denied) return denied;

  const canCreateOrgUnit = hasPermission(session, ORG_UNITS_PERMISSIONS.Create);
  const canUpdateOrgUnit = hasPermission(session, ORG_UNITS_PERMISSIONS.Update);
  const canDeleteOrgUnit = hasPermission(session, ORG_UNITS_PERMISSIONS.Delete);
  const canViewLevels = hasPermission(session, EMPLOYEE_LEVELS_PERMISSIONS.View);
  const canCreateLevel = hasPermission(session, EMPLOYEE_LEVELS_PERMISSIONS.Create);
  const canUpdateLevel = hasPermission(session, EMPLOYEE_LEVELS_PERMISSIONS.Update);
  const canDeleteLevel = hasPermission(session, EMPLOYEE_LEVELS_PERMISSIONS.Delete);

  const companiesResult = await searchCompanies({ pageSize: COMPANY_PAGE_SIZE });
  const companies = companiesResult.data?.records ?? [];

  const { companyId: requestedCompanyId } = await searchParams;
  const companyId = requestedCompanyId || companies[0]?.id || "";

  const [treeResult, levelsResult] = companyId
    ? await Promise.all([getOrgUnitTree(companyId), getEmployeeLevels(companyId)])
    : [null, null];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Departments &amp; Teams</h1>
        <p className="text-sm text-muted-foreground">
          Manage each company&apos;s department/team hierarchy and its employee levels.
        </p>
      </div>

      <CompanyFilter companies={companies} companyId={companyId} />

      {!companyId ? (
        <Card>
          <CardContent>
            <Empty>
              <EmptyTitle>No companies yet</EmptyTitle>
              <EmptyDescription>
                Create a company first under Organization &gt; Companies.
              </EmptyDescription>
            </Empty>
          </CardContent>
        </Card>
      ) : (
        <Tabs defaultValue="tree">
          <TabsList>
            <TabsTrigger value="tree">Departments &amp; Teams</TabsTrigger>
            {canViewLevels && <TabsTrigger value="levels">Employee Levels</TabsTrigger>}
          </TabsList>

          <TabsContent value="tree">
            <Card>
              <CardContent>
                <OrgUnitTree
                  companyId={companyId}
                  nodes={treeResult?.data ?? []}
                  error={
                    treeResult && !treeResult.isSuccess
                      ? treeResult.message
                      : undefined
                  }
                  canCreate={canCreateOrgUnit}
                  canUpdate={canUpdateOrgUnit}
                  canDelete={canDeleteOrgUnit}
                />
              </CardContent>
            </Card>
          </TabsContent>

          {canViewLevels && (
            <TabsContent value="levels">
              <Card>
                <CardContent>
                  <EmployeeLevelsPanel
                    companyId={companyId}
                    levels={levelsResult?.data ?? []}
                    error={
                      levelsResult && !levelsResult.isSuccess
                        ? levelsResult.message
                        : undefined
                    }
                    canCreate={canCreateLevel}
                    canUpdate={canUpdateLevel}
                    canDelete={canDeleteLevel}
                  />
                </CardContent>
              </Card>
            </TabsContent>
          )}
        </Tabs>
      )}
    </div>
  );
}
