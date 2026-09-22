import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Undo2 } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  GoodsReceiptStatus,
  GoodsReceiptStatusBadge,
  PURCHASING_PERMISSIONS,
  RefreshButton,
  isNumericId,
} from "@/modules/purchasing/common";
import { getGoodsReceiptById } from "@/modules/purchasing/goods-receipts/api/goods-receipts.api";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00 — null/undefined renders blank. */
function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface GoodsReceiptDetailPageProps {
  id: string;
}

export async function GoodsReceiptDetailPage({ id }: GoodsReceiptDetailPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Receipts.View);
  if (denied) return denied;

  if (!isNumericId(id)) notFound();

  // Returning needs returns.create to make the draft and returns.view to open it afterwards.
  const canReturn =
    hasPermission(session, PURCHASING_PERMISSIONS.Returns.Create) &&
    hasPermission(session, PURCHASING_PERMISSIONS.Returns.View);
  const canViewOrders = hasPermission(session, PURCHASING_PERMISSIONS.Orders.View);

  const detail = await getGoodsReceiptById(id);
  if (detail.code === "not_found") notFound();

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Goods receipt not available</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              {detail.message || "This goods receipt does not exist, or you do not have access to it."}
            </p>
            <Link
              href="/purchasing/receipts"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to goods receipts
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const receipt = detail.data;
  const lines = receipt.lines ?? [];
  const processing = receipt.status === GoodsReceiptStatus.Posting;
  const showReturn = canReturn && receipt.status === GoodsReceiptStatus.Posted;

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/purchasing/receipts"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to goods receipts
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex min-w-0 flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">{receipt.receiptNumber}</h1>
                <GoodsReceiptStatusBadge status={receipt.status} />
              </div>
              <p className="text-sm text-muted-foreground">
                {`${receipt.supplierName} · received at ${receipt.locationName}`}
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              {processing && <RefreshButton />}
              {showReturn && (
                <Button asChild size="sm">
                  <Link href={`/purchasing/returns/new?receiptId=${receipt.id}`}>
                    <Undo2 className="size-4" />
                    Return items
                  </Link>
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {processing && (
            <Alert>
              <AlertTitle>Processing</AlertTitle>
              <AlertDescription>
                The stock movement for this receipt is still being finalized. Use Refresh to check again.
              </AlertDescription>
            </Alert>
          )}

          {receipt.status === GoodsReceiptStatus.Voided && (
            <Alert variant="destructive">
              <AlertTitle>Voided</AlertTitle>
              <AlertDescription>
                <span>
                  {receipt.voidedAt && (
                    <>
                      Voided on <LocalDateTime value={receipt.voidedAt} />.{" "}
                    </>
                  )}
                  {receipt.voidReason}
                </span>
              </AlertDescription>
            </Alert>
          )}

          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Purchase order">
              {canViewOrders ? (
                <Link
                  href={`/purchasing/orders/${receipt.purchaseOrderId}`}
                  className="text-primary hover:underline"
                >
                  {receipt.poNumber}
                </Link>
              ) : (
                receipt.poNumber
              )}
            </Field>
            <Field label="Delivery note">{receipt.deliveryNoteRef}</Field>
            <Field label="Received">
              <LocalDateTime value={receipt.receivedAt} />
            </Field>
            <Field label="Total quantity">{formatQuantity(receipt.totalQuantity)}</Field>
            <Field label="Total cost">{formatMoney(receipt.totalCostBase)}</Field>
            {receipt.stockPostedAt && (
              <Field label="Stock posted">
                <LocalDateTime value={receipt.stockPostedAt} />
              </Field>
            )}
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Lines</CardTitle>
        </CardHeader>
        <CardContent>
          {lines.length === 0 ? (
            <p className="text-sm text-muted-foreground">No lines.</p>
          ) : (
            <>
              <div className="hidden md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Product</TableHead>
                      <TableHead className="text-right">Quantity</TableHead>
                      <TableHead className="text-right">Unit cost</TableHead>
                      <TableHead className="text-right">Line total</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {lines.map((line) => (
                      <TableRow key={line.id}>
                        <TableCell>
                          <div className="flex flex-col">
                            <span className="font-medium">{line.productName}</span>
                            <span className="text-xs text-muted-foreground">{line.sku}</span>
                          </div>
                        </TableCell>
                        <TableCell className="text-right">{formatQuantity(line.quantity)}</TableCell>
                        <TableCell className="text-right">{formatMoney(line.unitCostBase)}</TableCell>
                        <TableCell className="text-right">{formatMoney(line.lineTotalBase)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              <ul className="flex flex-col gap-3 md:hidden">
                {lines.map((line) => (
                  <li key={line.id} className="flex flex-col gap-2 rounded-lg border border-border p-3">
                    <div className="flex flex-col">
                      <span className="font-medium">{line.productName}</span>
                      <span className="text-xs text-muted-foreground">{line.sku}</span>
                    </div>
                    <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                      <Metric label="Quantity" value={formatQuantity(line.quantity)} />
                      <Metric label="Unit cost" value={formatMoney(line.unitCostBase)} />
                      <Metric label="Line total" value={formatMoney(line.lineTotalBase)} />
                    </dl>
                  </li>
                ))}
              </ul>
            </>
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

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-2">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}
