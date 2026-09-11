import { Badge } from "@/components/ui/badge";
import { getUserStatusTone } from "@/lib/shared/user-status";

const TONE_CLASSES: Record<"success" | "warning", string> = {
  success:
    "border-green-200 bg-green-50 text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-400",
  warning:
    "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-400",
};

export function UserStatusBadge({ status }: { status?: string | null }) {
  const tone = getUserStatusTone(status);
  const label = status ?? "Unknown";

  if (tone === "success" || tone === "warning") {
    return <Badge className={TONE_CLASSES[tone]}>{label}</Badge>;
  }

  return <Badge variant={tone === "danger" ? "destructive" : "outline"}>{label}</Badge>;
}
