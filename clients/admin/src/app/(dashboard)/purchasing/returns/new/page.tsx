import { CreatePurchaseReturnPage } from "@/modules/purchasing/purchase-returns";

export default function Page({ searchParams }: { searchParams: Promise<{ receiptId?: string }> }) {
  return <CreatePurchaseReturnPage searchParams={searchParams} />;
}
