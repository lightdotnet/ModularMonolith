"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import {
  RecordRateDialog,
  type RateCurrencyOption,
} from "@/modules/currency/exchange-rates/components/record-rate-dialog";
import type { ExchangeRateDto } from "@/modules/currency/exchange-rates/types/exchange-rate";
import { formatRate } from "@/modules/currency/exchange-rates/utils/format-rate";

interface LatestRatesPanelProps {
  /** The rate in effect per active foreign currency (`exchange_rate/latest`). */
  latest: ExchangeRateDto[];
  /** Active non-base currencies (for currencies that have no rate yet, and the dialog picker). */
  currencies: RateCurrencyOption[];
  baseCode?: string;
  lookupFailed?: boolean;
  truncated?: boolean;
  canManage?: boolean;
  /** The latest-rates call failed; shown instead of an unexplained empty panel. */
  error?: string;
}

interface Row {
  code: string;
  name?: string;
  rate?: ExchangeRateDto;
}

export function LatestRatesPanel({
  latest,
  currencies,
  baseCode,
  lookupFailed,
  truncated,
  canManage,
  error,
}: LatestRatesPanelProps) {
  const [recording, setRecording] = useState<string | null>(null);
  const [open, setOpen] = useState(false);

  const latestByCode = new Map(latest.map((rate) => [rate.currencyCode, rate]));
  const rows: Row[] =
    currencies.length > 0
      ? currencies.map((currency) => ({
          code: currency.code,
          name: currency.name,
          rate: latestByCode.get(currency.code),
        }))
      : latest.map((rate) => ({ code: rate.currencyCode, rate }));

  function startRecording(code: string) {
    setRecording(code);
    setOpen(true);
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Latest rates</CardTitle>
        <p className="text-sm text-muted-foreground">
          The rate currently in effect for each active foreign currency
          {baseCode ? `, in ${baseCode}` : ""}.
        </p>
      </CardHeader>
      <CardContent>
        {error ? (
          <p className="text-sm text-destructive">{error}</p>
        ) : rows.length === 0 ? (
          <p className="text-sm text-muted-foreground">No active foreign currencies.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {rows.map((row) => (
              <li key={row.code} className="flex items-start justify-between gap-3 rounded-lg border p-3">
                <div className="flex min-w-0 flex-col gap-0.5">
                  <span className="font-medium">
                    {row.rate
                      ? `1 ${row.code} = ${formatRate(row.rate.rate)} ${baseCode ?? ""}`.trim()
                      : row.code}
                  </span>
                  {row.name && <span className="truncate text-xs text-muted-foreground">{row.name}</span>}
                  {row.rate ? (
                    <span className="text-xs text-muted-foreground">
                      Since <LocalDateTime value={row.rate.effectiveFrom} />
                    </span>
                  ) : (
                    <span className="text-xs text-muted-foreground">No rate in effect yet</span>
                  )}
                </div>
                {canManage && (
                  <Button
                    aria-label={`Record new rate for ${row.code}`}
                    size="icon"
                    variant="outline"
                    className="shrink-0"
                    onClick={() => startRecording(row.code)}
                  >
                    <Plus />
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
      </CardContent>
      {canManage && (
        <RecordRateDialog
          key={open ? `open-${recording}` : "closed"}
          open={open}
          onOpenChange={setOpen}
          currencies={currencies}
          lookupFailed={lookupFailed}
          truncated={truncated}
          baseCode={baseCode}
          initialCurrencyCode={recording ?? undefined}
        />
      )}
    </Card>
  );
}
