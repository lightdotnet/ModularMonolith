"use client";

import { useEffect, useState } from "react";
import { Ban } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import { NumberInput } from "@/components/ui/number-input";
import { Spinner } from "@/components/ui/spinner";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { getPaymentsAction } from "@/modules/orders/api/get-payments-action";
import { recordPaymentAction } from "@/modules/orders/api/record-payment-action";
import { VoidPaymentDialog } from "@/modules/orders/components/void-payment-dialog";
import { OrderStatus, type OrderDto, type PaymentDto } from "@/modules/orders/types/order";
import { OrderTypeStatus, type OrderTypeDto } from "@/modules/orders/types/order-type";

const AMOUNT_FORMAT = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** #,##0.00 followed by the ISO currency code. */
function formatAmount(amount: number, currency: string): string {
  return `${AMOUNT_FORMAT.format(amount)} ${currency}`;
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

interface PaymentsSectionProps {
  order: OrderDto;
  canView: boolean;
  canManage: boolean;
  refresh: () => void;
  paymentTypes: OrderTypeDto[];
}

/**
 * Payments aren't nested on `OrderDto` (separate backend aggregate/controller), so this section
 * owns its own fetch+refetch instead of reading off `order` like `OrderFeeList`/`OrderLineList`
 * do. `refresh` is still called after every mutation here too, since recording/voiding a payment
 * can move `order.status`/`amountPaid`, which the summary footer and outer orders list both show.
 */
export function PaymentsSection({
  order,
  canView,
  canManage,
  refresh,
  paymentTypes,
}: PaymentsSectionProps) {
  const [payments, setPayments] = useState<PaymentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [recording, runRecord] = useGuardedAction();
  const [voidDialogPaymentId, setVoidDialogPaymentId] = useState<string | null>(null);

  const activePaymentTypes = paymentTypes.filter((t) => t.status === OrderTypeStatus.Active);

  const [amount, setAmount] = useState("");
  const [paymentTypeId, setPaymentTypeId] = useState(activePaymentTypes[0]?.id ?? "");
  const [paidAt, setPaidAt] = useState(today());
  const [reference, setReference] = useState("");

  /** Silent refetch used after a mutation — mirrors `OrderPanel`'s own `refresh()` (no loading toggle, that's only for the initial mount fetch below). */
  async function fetchPayments() {
    const result = await getPaymentsAction(order.id);
    if (!result.data) {
      setLoadError(result.error || "Unable to load payments.");
      return;
    }

    setLoadError("");
    setPayments(result.data);
  }

  useEffect(() => {
    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getPaymentsAction(order.id);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load payments.");
        setLoading(false);
        return;
      }

      setPayments(result.data);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [order.id]);

  if (!canView) return null;

  const canRecordPayment =
    canManage &&
    (order.status === OrderStatus.Placed || order.status === OrderStatus.PartiallyPaid);

  function handleRecord() {
    const parsedAmount = Number(amount);
    if (!amount || Number.isNaN(parsedAmount) || parsedAmount <= 0 || !paidAt || !paymentTypeId) return;

    runRecord(
      () =>
        recordPaymentAction(order.id, {
          amount: parsedAmount,
          currency: order.currency,
          paymentTypeId,
          paidAt: new Date(paidAt).toISOString(),
          reference: reference.trim() || undefined,
        }),
      "Payment recorded.",
      () => {
        setAmount("");
        setPaymentTypeId(activePaymentTypes[0]?.id ?? "");
        setPaidAt(today());
        setReference("");
        fetchPayments();
        refresh();
      },
    );
  }

  function handleVoided() {
    fetchPayments();
    refresh();
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm font-medium">Payments</span>

      {loading && payments.length === 0 ? (
        <div className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
          <Spinner />
          Loading payments...
        </div>
      ) : loadError ? (
        <p className="text-sm text-destructive">{loadError}</p>
      ) : payments.length === 0 ? (
        <p className="text-sm text-muted-foreground">No payments yet.</p>
      ) : (
        <div className="flex flex-col gap-1">
          {payments.map((payment) => (
            <div
              key={payment.id}
              className="flex flex-col gap-1 rounded-md border border-border px-2.5 py-1.5"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs text-muted-foreground">
                  {new Date(payment.paidAt).toLocaleString()}
                </span>
                {payment.isVoided && <Badge variant="destructive">Voided</Badge>}
              </div>
              <div className="flex items-center justify-between gap-2">
                <div className="flex min-w-0 items-center gap-2">
                  <span className="text-sm">{payment.paymentTypeName}</span>
                  {payment.reference && (
                    <span className="max-w-32 truncate text-xs text-muted-foreground">{payment.reference}</span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium">{formatAmount(payment.amount, payment.currency)}</span>
                  {canManage && !payment.isVoided && (
                    <Button
                      aria-label="Void payment"
                      onClick={() => setVoidDialogPaymentId(payment.id)}
                      size="icon-xs"
                      type="button"
                      variant="outline"
                    >
                      <Ban />
                    </Button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {canRecordPayment && (
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
          <div>
            <NumberInput aria-label="Payment amount" autoWidth value={amount} onValueChange={setAmount} />
          </div>
          <div className="w-36">
            <NativeSelect
              aria-label="Payment type"
              value={paymentTypeId}
              onChange={setPaymentTypeId}
              options={activePaymentTypes.map((t) => ({ value: t.id, label: t.name }))}
            />
          </div>
          <div className="w-20">
            <Input aria-label="Currency" value={order.currency} disabled />
          </div>
          <div className="w-40">
            <Input
              aria-label="Paid at"
              type="date"
              value={paidAt}
              onChange={(event) => setPaidAt(event.target.value)}
            />
          </div>
          <div className="flex-1">
            <Input
              aria-label="Reference"
              placeholder="Reference"
              maxLength={200}
              value={reference}
              onChange={(event) => setReference(event.target.value)}
            />
          </div>
          <Button
            type="button"
            loading={recording}
            disabled={!amount || !paidAt || !paymentTypeId}
            onClick={handleRecord}
          >
            Record payment
          </Button>
        </div>
      )}

      <VoidPaymentDialog
        open={voidDialogPaymentId !== null}
        onOpenChange={(open) => !open && setVoidDialogPaymentId(null)}
        paymentId={voidDialogPaymentId ?? ""}
        onVoided={handleVoided}
      />
    </div>
  );
}
