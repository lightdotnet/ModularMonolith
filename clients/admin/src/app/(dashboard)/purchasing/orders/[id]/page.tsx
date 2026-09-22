import { PurchaseOrderDetailPage } from "@/modules/purchasing/purchase-orders";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PurchaseOrderDetailPage id={id} />;
}
