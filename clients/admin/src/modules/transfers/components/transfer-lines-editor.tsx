"use client";

import { useRouter } from "next/navigation";
import { AddTransferLineForm } from "@/modules/transfers/components/add-transfer-line-form";
import { TransferLineRow } from "@/modules/transfers/components/transfer-line-row";
import type { TransferLineDto } from "@/modules/transfers/types/transfer";

interface TransferLinesEditorProps {
  transferId: string;
  lines: TransferLineDto[];
}

/** Draft-only line builder: the server page re-renders (via revalidation + `router.refresh()`) after every mutation. */
export function TransferLinesEditor({ transferId, lines }: TransferLinesEditorProps) {
  const router = useRouter();
  const refresh = () => router.refresh();

  return (
    <div className="flex flex-col gap-4">
      <AddTransferLineForm transferId={transferId} refresh={refresh} />

      {lines.length === 0 ? (
        <p className="text-sm text-muted-foreground">No lines yet. Add a product to get started.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {lines.map((line) => (
            // Keyed by quantity too, so a server-side correction remounts the row's local input state.
            <TransferLineRow
              key={`${line.id}-${line.requestedQuantity}`}
              transferId={transferId}
              line={line}
              refresh={refresh}
            />
          ))}
        </ul>
      )}
    </div>
  );
}
