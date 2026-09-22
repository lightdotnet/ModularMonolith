import { PurchaseReturnDetailPage } from "@/modules/purchasing/purchase-returns";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PurchaseReturnDetailPage id={id} />;
}
