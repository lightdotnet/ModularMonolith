import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { INVENTORY_STOCK_PERMISSIONS } from "@/modules/inventory";
import { getGoodsReceiptById } from "@/modules/purchasing/goods-receipts";
import {
  PURCHASING_PERMISSIONS,
  PurchaseReturnStatus,
  PurchaseReturnStatusBadge,
  formatPurchaseReturnReason,
  isNumericId,
} from "@/modules/purchasing/common";
import {
  getClaimedReturnQuantities,
  getPurchaseReturnById,
} from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import { PurchaseReturnDetailActions } from "@/modules/purchasing/purchase-returns/components/purchase-return-detail-actions";
import { PurchaseReturnForm } from "@/modules/purchasing/purchase-returns/components/purchase-return-form";
import { PurchaseReturnLinesTable } from "@/modules/purchasing/purchase-returns/components/purchase-return-lines-table";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00 — null/undefined renders blank. */
function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface PurchaseReturnDetailPageProps {
  id: string;
}

export async function PurchaseReturnDetailPage({ id }: PurchaseReturnDetailPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Returns.View);
  if (denied) return denied;

  if (!isNumericId(id)) notFound();

  const canCreate = hasPermission(session, PURCHASING_PERMISSIONS.Returns.Create);
  const canCredit = hasPermission(session, PURCHASING_PERMISSIONS.Returns.Credit);
  const canViewReceipts = hasPermission(session, PURCHASING_PERMISSIONS.Receipts.View);
  const canViewOrders = hasPermission(session, PURCHASING_PERMISSIONS.Orders.View);
  const canViewCost = hasPermission(session, INVENTORY_STOCK_PERMISSIONS.ViewCost);

  const detail = await getPurchaseReturnById(id);
  if (detail.code === "not_found") notFound();

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Purchase return not available</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              {detail.message || "This purchase return does not exist, or you do not have access to it."}
            </p>
            <Link
              href="/purchasing/returns"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to purchase returns
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const purchaseReturn = detail.data;
  const lines = purchaseReturn.lines ?? [];
  const isDraft = purchaseReturn.status === PurchaseReturnStatus.Draft;
  const editable = isDraft && canCreate;
  const processing = purchaseReturn.status === PurchaseReturnStatus.Posting;

  // The draft editor needs every receipt line (with its received quantity) to pick from.
  const receiptResult =
    editable && canViewReceipts ? await getGoodsReceiptById(purchaseReturn.goodsReceiptId) : null;
  const receiptLines = receiptResult?.data?.lines ?? null;
  // Exclude this draft's own claim so its current quantities stay editable.
  const claimed = editable
    ? (await getClaimedReturnQuantities(purchaseReturn.goodsReceiptId, purchaseReturn.id)).claimed
    : null;
  const draftByReceiptLine = new Map(lines.map((line) => [String(line.goodsReceiptLineId), line]));
  const candidates = (
    receiptLines
      ? receiptLines.map((line) => ({
          goodsReceiptLineId: String(line.id),
          productName: line.productName,
          sku: line.sku,
          receiptQuantity: line.quantity as number | undefined,
          returnableQuantity: claimed
            ? (Math.max(0, line.quantity - (claimed[String(line.id)] ?? 0)) as number | undefined)
            : undefined,
        }))
      : lines.map((line) => ({
          goodsReceiptLineId: String(line.goodsReceiptLineId),
          productName: line.productName,
          sku: line.sku,
          receiptQuantity: undefined as number | undefined,
          returnableQuantity: undefined as number | undefined,
        }))
  ).map((candidate) => {
    const existing = draftByReceiptLine.get(candidate.goodsReceiptLineId);
    return {
      ...candidate,
      initialQuantity: existing?.quantity ?? 0,
      initialReason: existing?.reason ?? "",
    };
  });

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/purchasing/returns"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to purchase returns
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex min-w-0 flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">{purchaseReturn.returnNumber}</h1>
                <PurchaseReturnStatusBadge status={purchaseReturn.status} />
              </div>
              <p className="text-sm text-muted-foreground">
                {`${purchaseReturn.supplierName} · from ${purchaseReturn.locationName}`}
              </p>
            </div>
            <PurchaseReturnDetailActions
              returnId={purchaseReturn.id}
              returnNumber={purchaseReturn.returnNumber}
              locationName={purchaseReturn.locationName}
              purchaseOrderId={purchaseReturn.purchaseOrderId}
              expectedCredit={purchaseReturn.expectedCreditBase}
              canPost={isDraft && canCreate && lines.length > 0}
              canCancel={isDraft && canCreate}
              canCredit={purchaseReturn.status === PurchaseReturnStatus.Posted && canCredit}
              processing={processing}
            />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {processing && (
            <Alert>
              <AlertTitle>Processing</AlertTitle>
              <AlertDescription>
                The stock movement for this return is still being finalized. No actions are available until it
                completes — use Refresh to check again.
              </AlertDescription>
            </Alert>
          )}

          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Goods receipt">
              {canViewReceipts ? (
                <Link
                  href={`/purchasing/receipts/${purchaseReturn.goodsReceiptId}`}
                  className="text-primary hover:underline"
                >
                  {purchaseReturn.receiptNumber}
                </Link>
              ) : (
                purchaseReturn.receiptNumber
              )}
            </Field>
            {canViewOrders && (
              <Field label="Purchase order">
                <Link
                  href={`/purchasing/orders/${purchaseReturn.purchaseOrderId}`}
                  className="text-primary hover:underline"
                >
                  Open purchase order
                </Link>
              </Field>
            )}
            <Field label="Reason">{formatPurchaseReturnReason(purchaseReturn.reason)}</Field>
            <Field label="Total quantity">{formatQuantity(purchaseReturn.totalQuantity)}</Field>
            <Field label="Expected credit">{formatMoney(purchaseReturn.expectedCreditBase)}</Field>
            {canViewCost && <Field label="Cost removed">{formatMoney(purchaseReturn.costRemovedBase)}</Field>}
            <Field label="Created">
              <LocalDateTime value={purchaseReturn.created} />
            </Field>
            {purchaseReturn.postedAt && (
              <Field label="Posted">
                <LocalDateTime value={purchaseReturn.postedAt} />
              </Field>
            )}
            {purchaseReturn.creditedAt && (
              <Field label="Credited">
                <LocalDateTime value={purchaseReturn.creditedAt} />
              </Field>
            )}
            {purchaseReturn.creditNoteNumber && (
              <Field label="Credit note">{purchaseReturn.creditNoteNumber}</Field>
            )}
            {purchaseReturn.creditedAt && (
              <Field label="Credit amount">{formatMoney(purchaseReturn.creditAmountBase)}</Field>
            )}
            {purchaseReturn.cancelledAt && (
              <Field label="Cancelled">
                <LocalDateTime value={purchaseReturn.cancelledAt} />
              </Field>
            )}
          </dl>

          {purchaseReturn.note && <Reason label="Note">{purchaseReturn.note}</Reason>}
          {purchaseReturn.cancelledReason && (
            <Reason label="Cancellation reason">{purchaseReturn.cancelledReason}</Reason>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{editable ? "Edit return" : "Lines"}</CardTitle>
        </CardHeader>
        <CardContent>
          {editable ? (
            <PurchaseReturnForm
              // Remounts when the saved draft changes so the inputs pick up server-side corrections.
              key={lines.map((line) => `${line.id}:${line.quantity}:${line.reason ?? ""}`).join("|")}
              goodsReceiptId={purchaseReturn.goodsReceiptId}
              returnId={purchaseReturn.id}
              initialReason={purchaseReturn.reason}
              initialNote={purchaseReturn.note ?? ""}
              candidates={candidates}
            />
          ) : (
            <PurchaseReturnLinesTable lines={lines} canViewCost={canViewCost} />
          )}
        </CardContent>
      </Card>
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
