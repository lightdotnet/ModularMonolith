import { Input } from "@/components/ui/input";
import { cn } from "@/lib/shared/utils";

interface ObjectViewerRowProps {
  label?: string;
  value: string;
  className?: string;
}

export function ObjectViewerRow({ label, value, className }: ObjectViewerRowProps) {
  return (
    <div
      className={cn(
        label &&
          "grid grid-cols-1 gap-1 sm:items-center sm:gap-3 sm:[grid-template-columns:var(--ov-label-w)_minmax(0,1fr)]",
        className,
      )}
    >
      {label ? (
        <span className="min-w-0 break-words text-sm font-medium text-foreground">
          {label}
        </span>
      ) : null}
      <Input disabled readOnly title={value} value={value} />
    </div>
  );
}
