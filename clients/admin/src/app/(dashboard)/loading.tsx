import { Spinner } from "@/components/ui/spinner";

export default function DashboardLoading() {
  return (
    <div className="flex justify-center py-12">
      <Spinner className="size-6 text-muted-foreground" />
    </div>
  );
}
