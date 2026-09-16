"use client";

import { useActionState, useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Spinner } from "@/components/ui/spinner";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { createOrderAction, type CreateOrderFormState } from "@/modules/orders/api/create-order-action";
import { getOrderByIdAction } from "@/modules/orders/api/get-order-by-id-action";
import { placeOrderAction } from "@/modules/orders/api/place-order-action";
import { AddOrderLineForm } from "@/modules/orders/components/add-order-line-form";
import { CancelOrderDialog } from "@/modules/orders/components/cancel-order-dialog";
import { LocationSelect } from "@/modules/orders/components/location-select";
import { OrderDiscountEditor } from "@/modules/orders/components/order-discount-editor";
import { OrderFeeList } from "@/modules/orders/components/order-fee-list";
import { OrderLineList } from "@/modules/orders/components/order-line-list";
import { OrderStatusBadge } from "@/modules/orders/components/order-status-badge";
import { OrderSummaryFooter } from "@/modules/orders/components/order-summary-footer";
import { OrderStatus, type OrderDto } from "@/modules/orders/types/order";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

const initialCreateState: CreateOrderFormState = {};

interface OrderPanelProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** `null` opens the panel in create mode (Phase A); a value resumes/views that order (Phase B). */
  orderId: string | null;
  locations: LocationTreeNodeDto[];
  /** Called after a mutation that should refresh the outer orders list (place/cancel — the two that change what the default list shows). */
  onChanged: () => void;
}

/**
 * Two-phase Dialog: Phase A (no order yet) is the create-draft form; on success it
 * transitions in-place to Phase B (the builder) without closing the dialog. Phase B
 * holds the one authoritative `order` fetched via `getOrderByIdAction`, refetched by
 * every child mutation through `refresh()`. Non-Draft orders fall through to the
 * same builder UI, just read-only (see each child component's `readOnly` guard) —
 * no separate view-only component.
 */
export function OrderPanel({ open, onOpenChange, orderId, locations, onChanged }: OrderPanelProps) {
  const [createState, createFormAction, creating] = useActionState(createOrderAction, initialCreateState);
  const [activeOrderId, setActiveOrderId] = useState<string | null>(orderId);
  const [locationId, setLocationId] = useState("");

  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [placing, runPlace] = useGuardedAction();
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);

  useActionSuccessToast(createState, "Order created.", () => {
    if (createState.orderId) setActiveOrderId(createState.orderId);
  });

  async function refresh() {
    if (!activeOrderId) return;

    const result = await getOrderByIdAction(activeOrderId);
    if (!result.data) {
      setLoadError(result.error || "Unable to load order.");
      return;
    }

    setLoadError("");
    setOrder(result.data);
  }

  useEffect(() => {
    if (!activeOrderId) return;
    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getOrderByIdAction(activeOrderId);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load order.");
        setLoading(false);
        return;
      }

      setOrder(result.data);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [activeOrderId]);

  function handlePlace() {
    if (!order) return;
    runPlace(
      () => placeOrderAction(order.id),
      "Order placed.",
      () => {
        refresh();
        onChanged();
      },
    );
  }

  function handleCancelled() {
    refresh();
    onChanged();
  }

  const readOnly = order ? order.status !== OrderStatus.Draft : false;

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent
          className="flex h-[85vh] w-full flex-col gap-0 overflow-hidden p-0 sm:max-w-4xl"
          onPointerDownOutside={(event) => event.preventDefault()}
        >
          {!activeOrderId ? (
            <form action={createFormAction} className="flex h-full flex-col gap-0">
              <DialogHeader className="border-b border-border p-4">
                <DialogTitle>Create order</DialogTitle>
              </DialogHeader>

              <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
                {createState.error && (
                  <Alert variant="destructive">
                    <AlertDescription>{createState.error}</AlertDescription>
                  </Alert>
                )}

                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="order-location">Location</Label>
                  <LocationSelect
                    id="order-location"
                    name="locationId"
                    value={locationId}
                    onValueChange={setLocationId}
                    locations={locations}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="order-member-id">Member ID</Label>
                  <Input id="order-member-id" name="memberId" />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="order-code">Order code</Label>
                  <Input id="order-code" name="orderCode" maxLength={17} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="order-external-ref">External reference code</Label>
                  <Input id="order-external-ref" name="externalReferenceCode" maxLength={50} />
                </div>
              </div>

              <div className="flex justify-end gap-2 border-t border-border p-4">
                <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                  Cancel
                </Button>
                <Button type="submit" loading={creating} disabled={!locationId}>
                  Create
                </Button>
              </div>
            </form>
          ) : (
            <div className="flex h-full flex-col">
              <DialogHeader className="border-b border-border p-4">
                <DialogTitle className="flex items-center gap-2">
                  {order?.orderCode ?? "Order"}
                  {order && <OrderStatusBadge status={order.status} />}
                </DialogTitle>
              </DialogHeader>

              {loading && !order ? (
                <div className="flex flex-1 items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
                  <Spinner />
                  Loading order...
                </div>
              ) : loadError && !order ? (
                <div className="flex-1 px-4">
                  <Alert variant="destructive">
                    <AlertDescription>{loadError}</AlertDescription>
                  </Alert>
                </div>
              ) : order ? (
                <>
                  <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
                    {loadError && (
                      <Alert variant="destructive">
                        <AlertDescription>{loadError}</AlertDescription>
                      </Alert>
                    )}

                    {!readOnly && <AddOrderLineForm orderId={order.id} refresh={refresh} />}
                    <OrderLineList order={order} readOnly={readOnly} refresh={refresh} />

                    <Separator />
                    <OrderDiscountEditor order={order} readOnly={readOnly} refresh={refresh} />

                    <Separator />
                    <OrderFeeList order={order} readOnly={readOnly} refresh={refresh} />
                  </div>

                  <OrderSummaryFooter
                    order={order}
                    placing={placing}
                    onPlace={handlePlace}
                    onCancel={() => setCancelDialogOpen(true)}
                  />
                </>
              ) : null}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <CancelOrderDialog
        open={cancelDialogOpen}
        onOpenChange={setCancelDialogOpen}
        orderId={activeOrderId ?? ""}
        onCancelled={handleCancelled}
      />
    </>
  );
}
