import { TransferDetailPage } from "@/modules/transfers";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <TransferDetailPage id={id} />;
}
