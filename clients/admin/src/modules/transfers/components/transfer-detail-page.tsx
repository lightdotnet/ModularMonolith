import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { INVENTORY_STOCK_PERMISSIONS } from "@/modules/inventory";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { getStockTransferById } from "@/modules/transfers/api/transfers.api";
import { TransferDetailActions } from "@/modules/transfers/components/transfer-detail-actions";
import { TransferLinesEditor } from "@/modules/transfers/components/transfer-lines-editor";
import { TransferLinesTable } from "@/modules/transfers/components/transfer-lines-table";
import { TransferReceiptsSection } from "@/modules/transfers/components/transfer-receipts-section";
import { TransferStatusBadge } from "@/modules/transfers/components/transfer-status-badge";
import { TRANSFERS_PERMISSIONS } from "@/modules/transfers/constants/permissions";
import { TransferReceiptStatus, TransferStatus } from "@/modules/transfers/types/transfer";
import { isNumericId } from "@/modules/transfers/utils/params";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00 — null/undefined renders blank. */
function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface TransferDetailPageProps {
  id: string;
}

export async function TransferDetailPage({ id }: TransferDetailPageProps) {
  const { session, denied } = await requirePermission(TRANSFERS_PERMISSIONS.View);
  if (denied) return denied;

  if (!isNumericId(id)) notFound();

  const canCreate = hasPermission(session, TRANSFERS_PERMISSIONS.Create);
  const canDispatch = hasPermission(session, TRANSFERS_PERMISSIONS.Dispatch);
  const canReceive = hasPermission(session, TRANSFERS_PERMISSIONS.Receive);
  const canClose = hasPermission(session, TRANSFERS_PERMISSIONS.Close);
  const canViewCost = hasPermission(session, INVENTORY_STOCK_PERMISSIONS.ViewCost);

  const detail = await getStockTransferById(id);

  if (detail.code === "not_found") notFound();

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Transfer not available</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              {detail.message || "This transfer does not exist, or you do not have access to it."}
            </p>
            <Link
              href="/transfers"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to transfers
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const transfer = detail.data;
  const isDraft = transfer.status === TransferStatus.Draft;
  const canEditDraft = isDraft && canCreate;
  const processing =
    transfer.status === TransferStatus.Posting ||
    transfer.receipts.some((receipt) => receipt.status === TransferReceiptStatus.Posting);

  // Location options are only needed by the Draft header editor.
  const locationTreeResult = canEditDraft ? await getLocationTree() : null;
  const locations = locationTreeResult?.data ? flattenLocationTree(locationTreeResult.data) : [];
  const locationsError =
    locationTreeResult && (!locationTreeResult.isSuccess || !locationTreeResult.data)
      ? locationTreeResult.message || "Unable to load locations."
      : undefined;

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/transfers"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to transfers
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex min-w-0 flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">{transfer.transferCode}</h1>
                <TransferStatusBadge status={transfer.status} />
              </div>
              <p className="flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground">
                {transfer.sourceLocationName}
                <ArrowRight className="size-3.5" aria-hidden />
                {transfer.destinationLocationName}
              </p>
            </div>
            <TransferDetailActions
              transfer={transfer}
              locations={locations}
              locationsUnavailable={!!locationsError}
              canCreate={canCreate}
              canDispatch={canDispatch}
              canReceive={canReceive}
              canClose={canClose}
            />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {locationsError && (
            <Alert variant="destructive">
              <AlertTitle>Unable to load locations</AlertTitle>
              <AlertDescription>
                {locationsError} Editing the transfer header is unavailable until this is resolved.
              </AlertDescription>
            </Alert>
          )}

          {processing && (
            <Alert>
              <AlertTitle>Processing</AlertTitle>
              <AlertDescription>
                The stock movement for this transfer is still being finalized. No actions are available
                until it completes — use Refresh to check again.
              </AlertDescription>
            </Alert>
          )}

          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Requested quantity">{formatQuantity(transfer.totalRequestedQuantity)}</Field>
            <Field label="In transit">{formatQuantity(transfer.totalInTransitQuantity)}</Field>
            <Field label="Created">
              <LocalDateTime value={transfer.requestedAt} />
            </Field>
            {transfer.dispatchedAt && (
              <Field label="Dispatched">
                <LocalDateTime value={transfer.dispatchedAt} />
              </Field>
            )}
            {transfer.receivedAt && (
              <Field label="Fully received">
                <LocalDateTime value={transfer.receivedAt} />
              </Field>
            )}
            {transfer.closedAt && (
              <Field label="Closed">
                <LocalDateTime value={transfer.closedAt} />
              </Field>
            )}
            {transfer.cancelledAt && (
              <Field label="Cancelled">
                <LocalDateTime value={transfer.cancelledAt} />
              </Field>
            )}
            {canViewCost && transfer.status === TransferStatus.Closed && (
              <Field label="Closed short value">{formatMoney(transfer.closedShortValueBase)}</Field>
            )}
          </dl>

          {transfer.note && (
            <Reason label="Note">{transfer.note}</Reason>
          )}
          {transfer.closedReason && <Reason label="Close reason">{transfer.closedReason}</Reason>}
          {transfer.cancelledReason && <Reason label="Cancellation reason">{transfer.cancelledReason}</Reason>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Lines</CardTitle>
        </CardHeader>
        <CardContent>
          {canEditDraft ? (
            <TransferLinesEditor transferId={transfer.id} lines={transfer.lines} />
          ) : (
            <TransferLinesTable lines={transfer.lines} canViewCost={canViewCost} />
          )}
        </CardContent>
      </Card>

      {!isDraft && (
        <Card>
          <CardHeader>
            <CardTitle>Receipts</CardTitle>
          </CardHeader>
          <CardContent>
            <TransferReceiptsSection transfer={transfer} />
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
