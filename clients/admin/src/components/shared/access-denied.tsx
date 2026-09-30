import { ShieldOff } from "lucide-react";
import { Empty, EmptyDescription, EmptyMedia, EmptyTitle } from "@/components/ui/empty";

interface AccessDeniedProps {
  permission: string;
}

export function AccessDenied({ permission }: AccessDeniedProps) {
  return (
    <Empty>
      <EmptyMedia variant="icon">
        <ShieldOff />
      </EmptyMedia>
      <EmptyTitle>Access denied</EmptyTitle>
      <EmptyDescription>You don&apos;t have the {permission} permission.</EmptyDescription>
    </Empty>
  );
}
