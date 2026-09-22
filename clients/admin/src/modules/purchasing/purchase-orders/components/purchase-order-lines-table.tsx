import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { PurchaseOrderLineDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface PurchaseOrderLinesTableProps {
  lines: PurchaseOrderLineDto[];
}

/**
 * Read-only lines view for every non-editable state. Desktop shows a table; below `md` the same
 * data is a stacked card list, so nothing is hidden behind horizontal scrolling.
 */
export function PurchaseOrderLinesTable({ lines }: PurchaseOrderLinesTableProps) {
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
              <TableHead className="text-right">Ordered</TableHead>
              <TableHead className="text-right">Received</TableHead>
              <TableHead className="text-right">Returned</TableHead>
              <TableHead className="text-right">Outstanding</TableHead>
              <TableHead className="text-right">Unit cost</TableHead>
              <TableHead className="text-right">Line total</TableHead>
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
                <TableCell className="text-right">{formatQuantity(line.orderedQuantity)}</TableCell>
                <TableCell className="text-right">{formatQuantity(line.receivedQuantity)}</TableCell>
                <TableCell className="text-right">{formatQuantity(line.returnedQuantity)}</TableCell>
                <TableCell className="text-right font-medium">{formatQuantity(line.outstandingQuantity)}</TableCell>
                <TableCell className="text-right">{formatMoney(line.unitCost)}</TableCell>
                <TableCell className="text-right">{formatMoney(line.lineTotal)}</TableCell>
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
              <Metric label="Ordered" value={formatQuantity(line.orderedQuantity)} />
              <Metric label="Received" value={formatQuantity(line.receivedQuantity)} />
              <Metric label="Returned" value={formatQuantity(line.returnedQuantity)} />
              <Metric label="Outstanding" value={formatQuantity(line.outstandingQuantity)} strong />
              <Metric label="Unit cost" value={formatMoney(line.unitCost)} />
              <Metric label="Line total" value={formatMoney(line.lineTotal)} />
            </dl>
          </li>
        ))}
      </ul>
    </>
  );
}

function Metric({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-2">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className={strong ? "font-medium" : undefined}>{value}</dd>
    </div>
  );
}
