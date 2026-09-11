import { ApprovalRequestDetailPage } from "@/modules/approvals";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <ApprovalRequestDetailPage id={id} />;
}
