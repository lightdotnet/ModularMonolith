import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  CURRENCY_PERMISSIONS,
  parseBooleanParam,
  parsePageNumber,
} from "@/modules/currency/common";
import { searchCurrencies } from "@/modules/currency/currencies/api/currencies.api";
import { CurrenciesDataTable } from "@/modules/currency/currencies/components/currencies-data-table";

const PAGE_SIZE = 10;

interface CurrenciesPageProps {
  searchParams: Promise<{ q?: string; page?: string; active?: string }>;
}

export async function CurrenciesPage({ searchParams }: CurrenciesPageProps) {
  const { session, denied } = await requirePermission(CURRENCY_PERMISSIONS.Currencies.View);
  if (denied) return denied;

  const canManage = hasPermission(session, CURRENCY_PERMISSIONS.Currencies.Manage);

  const { q, page, active } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const activeFilter = parseBooleanParam(active);
  const searchValue = q?.trim() || undefined;

  const result = await searchCurrencies({
    searchValue,
    isActive: activeFilter,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load currencies", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Currencies</h1>
        <p className="text-sm text-muted-foreground">
          The currencies products can be priced in. Exactly one is the base currency: it is the currency of
          every order, cannot be deactivated, and its decimal places cannot change.
        </p>
      </div>

      <Card>
        <CardContent>
          <CurrenciesDataTable
            searchValue={searchValue ?? ""}
            active={activeFilter === undefined ? "" : String(activeFilter)}
            records={paged?.records ?? []}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canManage={canManage}
          />
        </CardContent>
      </Card>
    </div>
  );
}
