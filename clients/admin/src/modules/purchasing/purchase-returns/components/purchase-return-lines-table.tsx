import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatPurchaseReturnReason } from "@/modules/purchasing/common";
import type { PurchaseReturnLineDto } from "@/modules/purchasing/purchase-returns/types/purchase-return";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface PurchaseReturnLinesTableProps {
  lines: PurchaseReturnLineDto[];
  /** The cost-removed column exists only with `inventory.stock.view_cost` — otherwise it is not rendered at all. */
  canViewCost: boolean;
}

/** Read-only lines view. Desktop shows a table; below `md` the same data is a stacked card list. */
export function PurchaseReturnLinesTable({ lines, canViewCost }: PurchaseReturnLinesTableProps) {
  if (lines.length === 0) {
    return <p className="text-sm text-muted-foreground">No lines.</p>;
  }

  return (
    <>
      <div className="hidden md:block">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Product</TableHead>
              <TableHead className="text-right">Quantity</TableHead>
              <TableHead>Reason</TableHead>
              <TableHead className="text-right">Receipt unit cost</TableHead>
              {canViewCost && <TableHead className="text-right">Cost removed</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {lines.map((line) => (
              <TableRow key={line.id}>
                <TableCell>
                  <div className="flex flex-col">
                    <span className="font-medium">{line.productName}</span>
                    <span className="text-xs text-muted-foreground">{line.sku}</span>
                  </div>
                </TableCell>
                <TableCell className="text-right">{formatQuantity(line.quantity)}</TableCell>
                <TableCell>{formatPurchaseReturnReason(line.reason)}</TableCell>
                <TableCell className="text-right">{formatMoney(line.receiptUnitCostBase)}</TableCell>
                {canViewCost && <TableCell className="text-right">{formatMoney(line.costRemovedBase)}</TableCell>}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <ul className="flex flex-col gap-3 md:hidden">
        {lines.map((line) => (
          <li key={line.id} className="flex flex-col gap-2 rounded-lg border border-border p-3">
            <div className="flex flex-col">
              <span className="font-medium">{line.productName}</span>
              <span className="text-xs text-muted-foreground">{line.sku}</span>
            </div>
            <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
              <Metric label="Quantity" value={formatQuantity(line.quantity)} />
              <Metric label="Reason" value={formatPurchaseReturnReason(line.reason)} />
              <Metric label="Receipt unit cost" value={formatMoney(line.receiptUnitCostBase)} />
              {canViewCost && <Metric label="Cost removed" value={formatMoney(line.costRemovedBase)} />}
            </dl>
          </li>
        ))}
      </ul>
    </>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-2">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}
