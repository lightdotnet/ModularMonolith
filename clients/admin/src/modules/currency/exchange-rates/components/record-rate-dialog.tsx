"use client";

import { useActionState, useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { CURRENCY_LIMITS } from "@/modules/currency/common/constants/limits";
import { recordExchangeRateAction } from "@/modules/currency/exchange-rates/api/record-exchange-rate-action";
import { buildRecordRateRequest } from "@/modules/currency/exchange-rates/utils/rate-form";

export interface RateCurrencyOption {
  code: string;
  name: string;
}

interface RecordRateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Active non-base currencies. */
  currencies: RateCurrencyOption[];
  /** The currency lookup failed — the dialog then falls back to a code input. */
  lookupFailed?: boolean;
  /** More currencies exist than were loaded into the picker. */
  truncated?: boolean;
  /** The base currency's code, when known. */
  baseCode?: string;
  /** Pre-selected currency (the per-currency shortcut). */
  initialCurrencyCode?: string;
}

/** Local time as a `datetime-local` input value (`yyyy-MM-ddTHH:mm`). */
function toLocalInputValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Record-rate dialog. The parent remounts it via `key` per open so form and action state reset. */
export function RecordRateDialog({
  open,
  onOpenChange,
  currencies,
  lookupFailed = false,
  truncated = false,
  baseCode,
  initialCurrencyCode,
}: RecordRateDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Record exchange rate</DialogTitle>
          <DialogDescription>
            Rates are append-only: a rate cannot be edited or deleted, a correction is recorded as a newer rate.
          </DialogDescription>
        </DialogHeader>
        <RecordRateForm
          currencies={currencies}
          lookupFailed={lookupFailed}
          truncated={truncated}
          baseCode={baseCode}
          initialCurrencyCode={initialCurrencyCode}
          onDone={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

function RecordRateForm({
  currencies,
  lookupFailed,
  truncated,
  baseCode,
  initialCurrencyCode,
  onDone,
}: {
  currencies: RateCurrencyOption[];
  lookupFailed: boolean;
  truncated: boolean;
  baseCode?: string;
  initialCurrencyCode?: string;
  onDone: () => void;
}) {
  const router = useRouter();
  const [state, formAction, pending] = useActionState(recordExchangeRateAction, {});
  const noForeignCurrency = !lookupFailed && currencies.length === 0;
  // Only pre-select a currency the select actually offers (active, non-base).
  const [currencyCode, setCurrencyCode] = useState(
    !lookupFailed && currencies.some((currency) => currency.code === initialCurrencyCode)
      ? (initialCurrencyCode ?? "")
      : "",
  );
  const [rateText, setRateText] = useState("");
  const [effectiveLocal, setEffectiveLocal] = useState(() => toLocalInputValue(new Date()));
  const [note, setNote] = useState("");

  useActionSuccessToast(state, "Exchange rate recorded.", () => {
    router.refresh();
    onDone();
  });

  const effectiveDate = effectiveLocal ? new Date(effectiveLocal) : null;
  const effectiveIso = effectiveDate && !Number.isNaN(effectiveDate.getTime()) ? effectiveDate.toISOString() : "";
  const validation = buildRecordRateRequest({ currencyCode, rateText, effectiveFrom: effectiveIso, note });
  const clientError = "error" in validation ? validation.error : undefined;
  // Surface a rule violation only once the user has started typing the rate, not on an empty fresh form.
  const showClientError = clientError && rateText.trim() !== "";
  const baseLabel = baseCode ?? "the base currency";
  const rateHint = `1 ${currencyCode.trim().toUpperCase() || "unit of the currency"} = ? ${baseLabel}`;

  return (
    <form action={formAction} className="flex flex-col gap-4">
      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        {lookupFailed ? (
          <>
            <Label htmlFor="rate-currency">Currency code</Label>
            <Input
              id="rate-currency"
              name="currencyCode"
              maxLength={CURRENCY_LIMITS.codeLength}
              value={currencyCode}
              onChange={(event) => setCurrencyCode(event.target.value.toUpperCase())}
              placeholder="USD"
              autoCapitalize="characters"
              required
            />
            <p className="text-xs text-muted-foreground">
              The currency list could not be loaded (you may need Currency access, currency.currencies.view),
              so enter the three-letter code of an active foreign currency.
            </p>
          </>
        ) : noForeignCurrency ? (
          <>
            <Label>Currency</Label>
            <p className="text-sm text-muted-foreground">
              Create and activate a foreign currency first. Rates can only be recorded for active currencies
              other than the base currency.
            </p>
          </>
        ) : (
          <NativeSelect
            id="rate-currency"
            label="Currency"
            name="currencyCode"
            placeholder="Select a currency"
            value={currencyCode}
            onChange={setCurrencyCode}
            options={currencies.map((currency) => ({
              value: currency.code,
              label: `${currency.code} - ${currency.name}`,
            }))}
            helperText={
              truncated
                ? "Only the first currencies are listed. Use the Currencies page to find others."
                : "Only active foreign currencies can have rates; the base currency and inactive currencies are rejected."
            }
            required
          />
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="rate-value">Rate</Label>
        <Input
          id="rate-value"
          name="rate"
          inputMode="decimal"
          autoComplete="off"
          value={rateText}
          onChange={(event) => setRateText(event.target.value)}
          aria-describedby="rate-value-hint"
          required
        />
        <p id="rate-value-hint" className="text-xs text-muted-foreground">
          {rateHint}. Greater than 0, up to {CURRENCY_LIMITS.maxRate.toLocaleString("en-US")}, at most{" "}
          {CURRENCY_LIMITS.rateScale} decimal places.
        </p>
        {showClientError && <p className="text-xs text-destructive">{clientError}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="rate-effective">Effective from</Label>
        <Input
          id="rate-effective"
          type="datetime-local"
          value={effectiveLocal}
          onChange={(event) => setEffectiveLocal(event.target.value)}
          required
        />
        <input type="hidden" name="effectiveFrom" value={effectiveIso} />
        <p className="text-xs text-muted-foreground">
          Must be later than this currency&apos;s newest recorded rate and at most 1 day in the future.
        </p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="rate-note">Note</Label>
        <Textarea
          id="rate-note"
          name="note"
          rows={2}
          maxLength={CURRENCY_LIMITS.noteMaxLength}
          value={note}
          onChange={(event) => setNote(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button type="submit" loading={pending} disabled={!!clientError || noForeignCurrency}>
          Record rate
        </Button>
      </DialogFooter>
    </form>
  );
}
