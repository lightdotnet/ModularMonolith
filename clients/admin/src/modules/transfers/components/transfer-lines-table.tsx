import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { TransferLineDto } from "@/modules/transfers/types/transfer";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** Money display is #0,000.00 — null/undefined renders blank. */
function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface TransferLinesTableProps {
  lines: TransferLineDto[];
  /** Cost columns exist only with `inventory.stock.view_cost` — otherwise they are not rendered at all. */
  canViewCost: boolean;
}

/**
 * Read-only lines view for every non-editable state. Desktop shows a table; below `md` the same
 * data is a stacked card list, so nothing is hidden behind horizontal scrolling.
 */
export function TransferLinesTable({ lines, canViewCost }: TransferLinesTableProps) {
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
              <TableHead className="text-right">Requested</TableHead>
              <TableHead className="text-right">Dispatched</TableHead>
              <TableHead className="text-right">Received</TableHead>
              <TableHead className="text-right">Closed short</TableHead>
              <TableHead className="text-right">In transit</TableHead>
              {canViewCost && <TableHead className="text-right">Unit cost</TableHead>}
              {canViewCost && <TableHead className="text-right">Closed short value</TableHead>}
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
                <TableCell className="text-right">{formatQuantity(line.requestedQuantity)}</TableCell>
                <TableCell className="text-right">{formatQuantity(line.qtyDispatched)}</TableCell>
                <TableCell className="text-right">{formatQuantity(line.qtyReceived)}</TableCell>
                <TableCell className="text-right">{formatQuantity(line.qtyClosedShort)}</TableCell>
                <TableCell className="text-right font-medium">{formatQuantity(line.qtyInTransit)}</TableCell>
                {canViewCost && <TableCell className="text-right">{formatMoney(line.unitCostBase)}</TableCell>}
                {canViewCost && (
                  <TableCell className="text-right">{formatMoney(line.closedShortValueBase)}</TableCell>
                )}
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
              <Metric label="Requested" value={formatQuantity(line.requestedQuantity)} />
              <Metric label="Dispatched" value={formatQuantity(line.qtyDispatched)} />
              <Metric label="Received" value={formatQuantity(line.qtyReceived)} />
              <Metric label="Closed short" value={formatQuantity(line.qtyClosedShort)} />
              <Metric label="In transit" value={formatQuantity(line.qtyInTransit)} strong />
              {canViewCost && <Metric label="Unit cost" value={formatMoney(line.unitCostBase)} />}
              {canViewCost && (
                <Metric label="Closed short value" value={formatMoney(line.closedShortValueBase)} />
              )}
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
