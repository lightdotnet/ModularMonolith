import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getGoodsReceiptById } from "@/modules/purchasing/goods-receipts";
import {
  GoodsReceiptStatus,
  PURCHASING_PERMISSIONS,
  parseNumericIdParam,
} from "@/modules/purchasing/common";
import { getClaimedReturnQuantities } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import { PurchaseReturnForm } from "@/modules/purchasing/purchase-returns/components/purchase-return-form";

interface CreatePurchaseReturnPageProps {
  searchParams: Promise<{ receiptId?: string }>;
}

/** Creates a Draft return from a Posted goods receipt (`?receiptId=`); the detail page then posts it. */
export async function CreatePurchaseReturnPage({ searchParams }: CreatePurchaseReturnPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Returns.Create);
  if (denied) return denied;

  const { receiptId } = await searchParams;
  const goodsReceiptId = parseNumericIdParam(receiptId);

  const receiptResult = goodsReceiptId ? await getGoodsReceiptById(goodsReceiptId) : null;
  const receipt = receiptResult?.data;

  // Returnable quantity = received minus other returns' claims; needs returns.view, otherwise the backend 409 is the guard.
  const claimed =
    receipt && goodsReceiptId && hasPermission(session, PURCHASING_PERMISSIONS.Returns.View)
      ? (await getClaimedReturnQuantities(goodsReceiptId)).claimed
      : null;

  let problem: string | null = null;
  if (!goodsReceiptId) {
    problem = "Open a posted goods receipt and choose Return items to start a return.";
  } else if (!receiptResult?.isSuccess || !receipt) {
    problem = receiptResult?.message || "The goods receipt could not be loaded.";
  } else if (receipt.status !== GoodsReceiptStatus.Posted) {
    problem = "Items can only be returned from a posted goods receipt.";
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        href={goodsReceiptId ? `/purchasing/receipts/${goodsReceiptId}` : "/purchasing/receipts"}
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to goods receipt
      </Link>

      {problem || !receipt || !goodsReceiptId ? (
        <Card>
          <CardHeader>
            <CardTitle>New purchase return</CardTitle>
          </CardHeader>
          <CardContent>
            <Alert>
              <AlertTitle>Cannot start a return</AlertTitle>
              <AlertDescription>{problem}</AlertDescription>
            </Alert>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>{`New return from ${receipt.receiptNumber}`}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {`${receipt.supplierName} · stock leaves ${receipt.locationName} when the return is posted.`}
            </p>
          </CardHeader>
          <CardContent>
            <PurchaseReturnForm
              goodsReceiptId={goodsReceiptId}
              initialReason=""
              initialNote=""
              candidates={(receipt.lines ?? []).map((line) => ({
                goodsReceiptLineId: String(line.id),
                productName: line.productName,
                sku: line.sku,
                receiptQuantity: line.quantity,
                returnableQuantity: claimed ? Math.max(0, line.quantity - (claimed[String(line.id)] ?? 0)) : undefined,
                initialQuantity: 0,
                initialReason: "",
              }))}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
