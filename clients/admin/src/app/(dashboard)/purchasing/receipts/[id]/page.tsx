import { GoodsReceiptDetailPage } from "@/modules/purchasing/goods-receipts";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <GoodsReceiptDetailPage id={id} />;
}
