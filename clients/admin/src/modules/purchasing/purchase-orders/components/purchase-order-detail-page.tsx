import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, ExternalLink } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import {
  EMPLOYEE_ID_CLAIM_TYPE,
  GoodsReceiptStatus,
  PURCHASING_PERMISSIONS,
  PurchaseOrderStatus,
  PurchaseOrderStatusBadge,
  formatDateOnly,
  LOCATION_HINT,
  LookupHints,
  SUPPLIER_ACCESS_HINT,
  SUPPLIER_FAILED_HINT,
  supplierTruncatedHint,
  isNumericId,
} from "@/modules/purchasing/common";
import { getPurchaseOrderById } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import {
  PurchaseOrderDetailActions,
  type PurchaseOrderActionFlags,
} from "@/modules/purchasing/purchase-orders/components/purchase-order-detail-actions";
import { PurchaseOrderLinesEditor } from "@/modules/purchasing/purchase-orders/components/purchase-order-lines-editor";
import { PurchaseOrderLinesTable } from "@/modules/purchasing/purchase-orders/components/purchase-order-lines-table";
import { PurchaseOrderReceiptsSection } from "@/modules/purchasing/purchase-orders/components/purchase-order-receipts-section";
import { SUPPLIER_OPTIONS_LIMIT, getSupplierOptions } from "@/modules/purchasing/suppliers";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00 — null/undefined renders blank. */
function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface PurchaseOrderDetailPageProps {
  id: string;
}

export async function PurchaseOrderDetailPage({ id }: PurchaseOrderDetailPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Orders.View);
  if (denied) return denied;

  if (!isNumericId(id)) notFound();

  const canCreate = hasPermission(session, PURCHASING_PERMISSIONS.Orders.Create);
  const canSubmit = hasPermission(session, PURCHASING_PERMISSIONS.Orders.Submit);
  const canClose = hasPermission(session, PURCHASING_PERMISSIONS.Orders.Close);
  const canReceive = hasPermission(session, PURCHASING_PERMISSIONS.Receipts.Create);
  const canViewReceipts = hasPermission(session, PURCHASING_PERMISSIONS.Receipts.View);
  const canViewSuppliers = hasPermission(session, PURCHASING_PERMISSIONS.Suppliers.View);
  const currentEmployeeId = session.claims.find((claim) => claim.type === EMPLOYEE_ID_CLAIM_TYPE)?.value;

  const detail = await getPurchaseOrderById(id);
  if (detail.code === "not_found") notFound();

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Purchase order not available</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              {detail.message || "This purchase order does not exist, or you do not have access to it."}
            </p>
            <Link
              href="/purchasing/orders"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to purchase orders
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const order = detail.data;
  const lines = order.lines ?? [];
  const receipts = order.receipts ?? [];

  const isRequester = !!currentEmployeeId && currentEmployeeId === order.requesterEmployeeId;
  const editableState =
    order.status === PurchaseOrderStatus.Draft || order.status === PurchaseOrderStatus.Rejected;
  const canEditContent = editableState && canCreate && (isRequester || canClose);
  const processing = receipts.some((receipt) => receipt.status === GoodsReceiptStatus.Posting);
  // Matches the backend: only received quantity (or an in-flight Posting receipt) blocks cancelling an Approved
  // order — a Voided receipt counts toward nothing.
  const nothingReceived =
    order.totalReceivedQuantity === 0 &&
    receipts.every((receipt) => receipt.status === GoodsReceiptStatus.Voided);

  const flags: PurchaseOrderActionFlags = {
    edit: canEditContent,
    submit: editableState && canSubmit && isRequester && lines.length > 0,
    withdraw: order.status === PurchaseOrderStatus.PendingApproval && canSubmit && isRequester,
    receive:
      !processing &&
      (order.status === PurchaseOrderStatus.Approved ||
        order.status === PurchaseOrderStatus.PartiallyReceived) &&
      canReceive &&
      order.totalOutstandingQuantity > 0,
    close: !processing && order.status === PurchaseOrderStatus.PartiallyReceived && canClose,
    cancel:
      !processing &&
      (canEditContent ||
        (order.status === PurchaseOrderStatus.Approved && canCreate && canClose && nothingReceived)),
  };

  // Header options are only needed by the header editor.
  const [locationTreeResult, supplierLookup] = canEditContent
    ? await Promise.all([
        getLocationTree(),
        canViewSuppliers ? getSupplierOptions(true) : null,
      ])
    : [null, null];
  const locations = locationTreeResult?.data ? flattenLocationTree(locationTreeResult.data) : [];
  const suppliers = supplierLookup?.options ?? [];

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/purchasing/orders"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to purchase orders
      </Link>

      {canEditContent && (
        <LookupHints
          hints={[
            !canViewSuppliers && SUPPLIER_ACCESS_HINT,
            supplierLookup?.failed && SUPPLIER_FAILED_HINT,
            supplierLookup?.truncated && supplierTruncatedHint(SUPPLIER_OPTIONS_LIMIT),
            locationTreeResult && !locationTreeResult.isSuccess && LOCATION_HINT,
          ]}
        />
      )}

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex min-w-0 flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">{order.poNumber}</h1>
                <PurchaseOrderStatusBadge status={order.status} />
              </div>
              <p className="text-sm text-muted-foreground">
                {`${order.supplierName} · deliver to ${order.locationName}`}
              </p>
            </div>
            <PurchaseOrderDetailActions
              order={order}
              flags={flags}
              processing={processing}
              locations={locations}
              suppliers={suppliers}
            />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {processing && (
            <Alert>
              <AlertTitle>Processing</AlertTitle>
              <AlertDescription>
                The stock movement for a receipt is still being finalized. No actions are available until it
                completes — use Refresh to check again.
              </AlertDescription>
            </Alert>
          )}

          {order.status === PurchaseOrderStatus.Rejected && (
            <Alert variant="destructive">
              <AlertTitle>Rejected</AlertTitle>
              <AlertDescription>
                <span>
                  This order was rejected{order.rejectedAt ? " on " : ""}
                  {order.rejectedAt && <LocalDateTime value={order.rejectedAt} />}. Edit it and resubmit to
                  start a new approval.
                </span>
              </AlertDescription>
            </Alert>
          )}

          {order.status === PurchaseOrderStatus.PendingApproval && (
            <Alert>
              <AlertTitle>Waiting for approval</AlertTitle>
              <AlertDescription>
                {order.approverName
                  ? `Waiting for ${order.approverName} to decide in the Approvals area.`
                  : "Waiting for the approver to decide in the Approvals area."}
              </AlertDescription>
            </Alert>
          )}

          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Total">{`${order.currency} ${formatMoney(order.totalAmount)}`}</Field>
            <Field label="Ordered quantity">{formatQuantity(order.totalOrderedQuantity)}</Field>
            <Field label="Received quantity">{formatQuantity(order.totalReceivedQuantity)}</Field>
            <Field label="Outstanding quantity">{formatQuantity(order.totalOutstandingQuantity)}</Field>
            <Field label="Expected delivery">{formatDateOnly(order.expectedAt)}</Field>
            <Field label="Created">
              <LocalDateTime value={order.created} />
            </Field>
            {order.approverName && <Field label="Approver">{order.approverName}</Field>}
            {order.submittedAt && (
              <Field label="Submitted">
                <LocalDateTime value={order.submittedAt} />
              </Field>
            )}
            {order.approvedAt && (
              <Field label="Approved">
                <LocalDateTime value={order.approvedAt} />
              </Field>
            )}
            {order.rejectedAt && (
              <Field label="Rejected">
                <LocalDateTime value={order.rejectedAt} />
              </Field>
            )}
            {order.receivedAt && (
              <Field label="Fully received">
                <LocalDateTime value={order.receivedAt} />
              </Field>
            )}
            {order.closedAt && (
              <Field label="Closed">
                <LocalDateTime value={order.closedAt} />
              </Field>
            )}
            {order.cancelledAt && (
              <Field label="Cancelled">
                <LocalDateTime value={order.cancelledAt} />
              </Field>
            )}
          </dl>

          {order.approvalRequestId && (
            <Link
              href={`/approvals/requests/${order.approvalRequestId}`}
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ExternalLink className="size-4" />
              View approval progress
            </Link>
          )}

          {order.note && <Reason label="Note">{order.note}</Reason>}
          {order.closedReason && <Reason label="Close reason">{order.closedReason}</Reason>}
          {order.cancelledReason && <Reason label="Cancellation reason">{order.cancelledReason}</Reason>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Lines</CardTitle>
        </CardHeader>
        <CardContent>
          {canEditContent ? (
            <PurchaseOrderLinesEditor purchaseOrderId={order.id} currency={order.currency} lines={lines} />
          ) : (
            <PurchaseOrderLinesTable lines={lines} />
          )}
        </CardContent>
      </Card>

      {editableState ? null : (
        <Card>
          <CardHeader>
            <CardTitle>Receipts</CardTitle>
          </CardHeader>
          <CardContent>
            <PurchaseOrderReceiptsSection receipts={receipts} canViewReceipts={canViewReceipts} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-sm">{children}</dd>
    </div>
  );
}

function Reason({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted-foreground">{label}</span>
      <p className="whitespace-pre-wrap break-words rounded-md border bg-muted/50 p-3 text-sm">{children}</p>
    </div>
  );
}
