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
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { CURRENCY_LIMITS } from "@/modules/currency/common/constants/limits";
import { createCurrencyAction } from "@/modules/currency/currencies/api/create-currency-action";
import { updateCurrencyAction } from "@/modules/currency/currencies/api/update-currency-action";
import type { CurrencyDto } from "@/modules/currency/currencies/types/currency";

const DECIMAL_OPTIONS = Array.from({ length: CURRENCY_LIMITS.maxDecimalPlaces + 1 }, (_, index) => ({
  value: String(index),
  label: String(index),
}));

interface CurrencyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Omit to create a new currency. */
  currency?: CurrencyDto;
}

/** Create/edit dialog. The parent remounts it via `key` per open so form and action state reset. */
export function CurrencyDialog({ open, onOpenChange, currency }: CurrencyDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{currency ? `Edit ${currency.code}` : "New currency"}</DialogTitle>
          <DialogDescription>
            {currency
              ? "The code cannot be changed."
              : "Use the three-letter ISO 4217 code (for example USD). New currencies start active."}
          </DialogDescription>
        </DialogHeader>
        <CurrencyForm currency={currency} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CurrencyForm({ currency, onDone }: { currency?: CurrencyDto; onDone: () => void }) {
  const router = useRouter();
  const [state, formAction, pending] = useActionState(
    currency ? updateCurrencyAction : createCurrencyAction,
    {},
  );
  const [code, setCode] = useState(currency?.code ?? "");
  const [name, setName] = useState(currency?.name ?? "");
  const [symbol, setSymbol] = useState(currency?.symbol ?? "");
  const [decimalPlaces, setDecimalPlaces] = useState(String(currency?.decimalPlaces ?? 2));

  useActionSuccessToast(state, currency ? "Currency updated." : "Currency created.", () => {
    router.refresh();
    onDone();
  });

  const isBase = currency?.isBase ?? false;

  return (
    <form action={formAction} className="flex flex-col gap-4">
      {currency && <input type="hidden" name="code" value={currency.code} />}

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="currency-code">Code</Label>
          <Input
            id="currency-code"
            name={currency ? undefined : "code"}
            maxLength={CURRENCY_LIMITS.codeLength}
            value={code}
            onChange={(event) => setCode(event.target.value.toUpperCase())}
            disabled={!!currency}
            className="uppercase"
            autoCapitalize="characters"
            required
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="currency-symbol">Symbol</Label>
          <Input
            id="currency-symbol"
            name="symbol"
            maxLength={CURRENCY_LIMITS.symbolMaxLength}
            value={symbol}
            onChange={(event) => setSymbol(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="currency-name">Name</Label>
        <Input
          id="currency-name"
          name="name"
          maxLength={CURRENCY_LIMITS.nameMaxLength}
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <NativeSelect
          id="currency-decimals"
          label="Decimal places"
          name={isBase ? undefined : "decimalPlaces"}
          value={decimalPlaces}
          onChange={setDecimalPlaces}
          options={DECIMAL_OPTIONS}
          disabled={isBase}
          helperText={
            isBase
              ? "The base currency's decimal places cannot be changed."
              : currency
                ? "Allowed from 0 to 4. Cannot be changed once this currency has recorded exchange rates."
                : "Allowed from 0 to 4 (for example 0 for VND or JPY, 2 for USD)."
          }
        />
        {/* A disabled control is not submitted, so the base currency's fixed value travels via a hidden field. */}
        {isBase && <input type="hidden" name="decimalPlaces" value={decimalPlaces} />}
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button
          type="submit"
          loading={pending}
          disabled={(!currency && code.trim().length !== CURRENCY_LIMITS.codeLength) || !name.trim()}
        >
          {currency ? "Save" : "Create"}
        </Button>
      </DialogFooter>
    </form>
  );
}
