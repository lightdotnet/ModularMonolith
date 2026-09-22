import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { searchStockTransfers } from "@/modules/transfers/api/transfers.api";
import { TransfersDataTable } from "@/modules/transfers/components/transfers-data-table";
import { TRANSFERS_PERMISSIONS } from "@/modules/transfers/constants/permissions";
import { parsePageNumber, parseTransferStatus } from "@/modules/transfers/utils/params";

const PAGE_SIZE = 10;
const MAX_LOCATION_ID_LENGTH = 450;

interface TransfersPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
    status?: string;
    sourceLocationId?: string;
    destinationLocationId?: string;
  }>;
}

export async function TransfersPage({ searchParams }: TransfersPageProps) {
  const { session, denied } = await requirePermission(TRANSFERS_PERMISSIONS.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, TRANSFERS_PERMISSIONS.Create);

  const { q, page, status, sourceLocationId, destinationLocationId } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const statusFilter = parseTransferStatus(status);
  const searchValue = q?.trim() || undefined;

  const locationTreeResult = await getLocationTree();
  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];
  const locationsError =
    !locationTreeResult.isSuccess || !locationTreeResult.data
      ? locationTreeResult.message || "Unable to load locations."
      : undefined;

  // Location ids are opaque strings — only accept ones that exist in the loaded location list.
  const knownLocationIds = new Set(locations.map((location) => String(location.id)));
  const safeLocationId = (value: string | undefined) =>
    value && value.length <= MAX_LOCATION_ID_LENGTH && knownLocationIds.has(value) ? value : undefined;
  const sourceFilter = safeLocationId(sourceLocationId);
  const destinationFilter = safeLocationId(destinationLocationId);

  const transfersResult = await searchStockTransfers({
    status: statusFilter,
    sourceLocationId: sourceFilter,
    destinationLocationId: destinationFilter,
    searchValue,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !transfersResult.isSuccess || !transfersResult.data
      ? {
          title: "Unable to load transfers",
          description: transfersResult.message || "Please try again.",
        }
      : undefined;
  const paged = transfersResult.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Transfers</h1>
        <p className="text-sm text-muted-foreground">
          Move stock between locations: dispatch from the source, then receive at the destination.
        </p>
      </div>

      {locationsError && (
        <Alert variant="destructive">
          <AlertTitle>Unable to load locations</AlertTitle>
          <AlertDescription>
            {locationsError} The location filters and creating a new transfer are unavailable until this
            is resolved.
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardContent>
          <TransfersDataTable
            locations={locations}
            status={statusFilter ?? ""}
            sourceLocationId={sourceFilter ?? ""}
            destinationLocationId={destinationFilter ?? ""}
            searchValue={searchValue ?? ""}
            records={paged?.records ?? []}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canCreate={canCreate && !locationsError}
          />
        </CardContent>
      </Card>
    </div>
  );
}
