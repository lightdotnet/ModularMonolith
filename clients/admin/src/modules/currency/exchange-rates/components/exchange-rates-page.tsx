import { Alert, AlertDescription } from "@/components/ui/alert";
import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  CURRENCY_OPTIONS_LIMIT,
  CURRENCY_PERMISSIONS,
  parseCurrencyCodeParam,
  parseDateParam,
  parsePageNumber,
} from "@/modules/currency/common";
import { getCurrencyOptions } from "@/modules/currency/currencies";
import {
  getLatestExchangeRates,
  searchExchangeRates,
} from "@/modules/currency/exchange-rates/api/exchange-rates.api";
import { ExchangeRatesDataTable } from "@/modules/currency/exchange-rates/components/exchange-rates-data-table";
import { LatestRatesPanel } from "@/modules/currency/exchange-rates/components/latest-rates-panel";

const PAGE_SIZE = 10;

interface ExchangeRatesPageProps {
  searchParams: Promise<{ currency?: string; from?: string; to?: string; page?: string }>;
}

export async function ExchangeRatesPage({ searchParams }: ExchangeRatesPageProps) {
  const { session, denied } = await requirePermission(CURRENCY_PERMISSIONS.Rates.View);
  if (denied) return denied;

  const canManage = hasPermission(session, CURRENCY_PERMISSIONS.Rates.Manage);

  const { currency, from, to, page } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const currencyCode = parseCurrencyCodeParam(currency);
  const fromDate = parseDateParam(from);
  // The page cannot know the viewer's time zone, so the filter days are UTC days (the fields are labelled "(UTC)").
  // An inverted range is rejected by the backend, so drop the "to" bound rather than showing an error.
  const toDate = parseDateParam(to);
  const validTo = fromDate && toDate && toDate < fromDate ? undefined : toDate;

  const [history, latest, currencies] = await Promise.all([
    searchExchangeRates({
      currencyCode,
      from: fromDate ? `${fromDate}T00:00:00.000Z` : undefined,
      to: validTo ? `${validTo}T23:59:59.999Z` : undefined,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
    getLatestExchangeRates(),
    getCurrencyOptions(),
  ]);

  const error =
    !history.isSuccess || !history.data
      ? { title: "Unable to load exchange rates", description: history.message || "Please try again." }
      : undefined;
  const paged = history.data;

  const baseCode = currencies.options.find((option) => option.isBase)?.code;
  const recordCurrencies = currencies.options
    .filter((option) => option.isActive && !option.isBase)
    .map((option) => ({ code: option.code, name: option.name }));
  const filterCurrencies = currencies.options.map((option) => ({ code: option.code, name: option.name }));

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Exchange rates</h1>
        <p className="text-sm text-muted-foreground">
          A rate means 1 unit of the currency = the rate in units of the base currency
          {baseCode ? ` (${baseCode})` : ""}. History is append-only: to correct a rate, record a newer one.
        </p>
      </div>

      {currencies.truncated && (
        <Alert>
          <AlertDescription>
            Only the first {CURRENCY_OPTIONS_LIMIT} currencies are listed in the currency pickers and the base
            currency may be missing. Use the Currencies page to find others.
          </AlertDescription>
        </Alert>
      )}

      <LatestRatesPanel
        latest={latest.isSuccess && latest.data ? latest.data : []}
        currencies={recordCurrencies}
        lookupFailed={currencies.failed}
        truncated={currencies.truncated}
        baseCode={baseCode}
        canManage={canManage}
        error={!latest.isSuccess ? latest.message || "Unable to load the latest rates." : undefined}
      />

      <Card>
        <CardContent>
          <ExchangeRatesDataTable
            currencyCode={currencyCode ?? ""}
            from={fromDate ?? ""}
            to={validTo ?? ""}
            records={paged?.records ?? []}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canManage={canManage}
            recordCurrencies={recordCurrencies}
            filterCurrencies={filterCurrencies}
            lookupFailed={currencies.failed}
            truncated={currencies.truncated}
            baseCode={baseCode}
          />
        </CardContent>
      </Card>
    </div>
  );
}
