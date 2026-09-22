import { Card, CardContent } from "@/components/ui/card";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { getLocationTypes } from "@/modules/location/api/location-types.api";
import { LocationsMasterDetail } from "@/modules/location/components/locations-master-detail";
import { LocationTypesPanel } from "@/modules/location/components/location-types-panel";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  LOCATION_TYPES_PERMISSIONS,
  LOCATIONS_PERMISSIONS,
} from "@/modules/location/constants/permissions";

export async function LocationsPage() {
  const { session, denied } = await requirePermission(LOCATIONS_PERMISSIONS.View);
  if (denied) return denied;

  const canManageLocations = hasPermission(session, LOCATIONS_PERMISSIONS.Manage);
  const canViewTypes = hasPermission(session, LOCATION_TYPES_PERMISSIONS.View);
  const canManageTypes = hasPermission(session, LOCATION_TYPES_PERMISSIONS.Manage);

  const [treeResult, typesResult] = await Promise.all([
    getLocationTree(),
    getLocationTypes(),
  ]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Locations</h1>
        <p className="text-sm text-muted-foreground">
          Manage location hierarchy and location types.
        </p>
      </div>

      <Tabs defaultValue="tree">
        <TabsList>
          <TabsTrigger value="tree">Locations</TabsTrigger>
          {canViewTypes && <TabsTrigger value="types">Location Types</TabsTrigger>}
        </TabsList>

        <TabsContent value="tree">
          <LocationsMasterDetail
            nodes={treeResult.data ?? []}
            locationTypes={typesResult.data ?? []}
            error={!treeResult.isSuccess ? treeResult.message : undefined}
            canManage={canManageLocations}
          />
        </TabsContent>

        {canViewTypes && (
          <TabsContent value="types">
            <Card>
              <CardContent>
                <LocationTypesPanel
                  locationTypes={typesResult.data ?? []}
                  error={!typesResult.isSuccess ? typesResult.message : undefined}
                  canManage={canManageTypes}
                />
              </CardContent>
            </Card>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
