"use client";

import { useActionState } from "react";
import { MapPin } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  CardAction,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Empty, EmptyDescription, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateLocationAction,
  type UpdateLocationFormState,
} from "@/modules/location/api/update-location-action";
import {
  flattenLocationTree,
  LocationStatus,
  type LocationTreeNodeDto,
} from "@/modules/location/types/location";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

const initialState: UpdateLocationFormState = {};

interface LocationDetailPanelProps {
  node: LocationTreeNodeDto | null;
  nodes: LocationTreeNodeDto[];
  locationTypes: LocationTypeDto[];
  canManage?: boolean;
  onMove: (node: LocationTreeNodeDto) => void;
  onDelete: (node: LocationTreeNodeDto) => void;
  onAddChild: (parent: LocationTreeNodeDto) => void;
  onUpdated: () => void;
}

export function LocationDetailPanel({
  node,
  nodes,
  locationTypes,
  canManage,
  onMove,
  onDelete,
  onAddChild,
  onUpdated,
}: LocationDetailPanelProps) {
  if (!node) {
    return (
      <CardContent>
        <Empty className="min-h-80">
          <EmptyMedia variant="icon">
            <MapPin />
          </EmptyMedia>
          <EmptyTitle>Select a location</EmptyTitle>
          <EmptyDescription>
            Choose a location from the tree to view and edit its details.
          </EmptyDescription>
        </Empty>
      </CardContent>
    );
  }

  return (
    <LocationDetailForm
      key={node.id}
      node={node}
      nodes={nodes}
      locationTypes={locationTypes}
      canManage={canManage}
      onMove={onMove}
      onDelete={onDelete}
      onAddChild={onAddChild}
      onUpdated={onUpdated}
    />
  );
}

interface LocationDetailFormProps {
  node: LocationTreeNodeDto;
  nodes: LocationTreeNodeDto[];
  locationTypes: LocationTypeDto[];
  canManage?: boolean;
  onMove: (node: LocationTreeNodeDto) => void;
  onDelete: (node: LocationTreeNodeDto) => void;
  onAddChild: (parent: LocationTreeNodeDto) => void;
  onUpdated: () => void;
}

function LocationDetailForm({
  node,
  nodes,
  locationTypes,
  canManage,
  onMove,
  onDelete,
  onAddChild,
  onUpdated,
}: LocationDetailFormProps) {
  const [state, formAction, pending] = useActionState(updateLocationAction, initialState);

  useActionSuccessToast(state, "Location updated.", onUpdated);

  const typeDto = locationTypes.find((t) => t.id === node.locationTypeId);
  const typeName = typeDto?.name ?? node.locationTypeId;
  const canHaveChildren = typeDto?.canHaveChildren ?? false;

  const parent = node.parentLocationId
    ? (flattenLocationTree(nodes).find((n) => n.id === node.parentLocationId) ?? null)
    : null;

  return (
    <>
      <CardHeader>
        <CardTitle>{node.name}</CardTitle>
        <CardAction>
          <Badge variant="outline">{typeName}</Badge>
        </CardAction>
      </CardHeader>

      <CardContent className="flex flex-col gap-4">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">Location Type</span>
          <span className="text-sm">{typeName}</span>
        </div>

        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">Parent</span>
          <div className="flex items-center gap-2">
            <span className="flex-1 text-sm">{parent ? parent.name : "— top-level —"}</span>
            {canManage && (
              <Button size="sm" variant="outline" onClick={() => onMove(node)}>
                Move
              </Button>
            )}
          </div>
        </div>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={node.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="detail-loc-code">Code</Label>
            <Input
              id="detail-loc-code"
              name="code"
              defaultValue={node.code}
              required
              disabled={!canManage}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="detail-loc-name">Name</Label>
            <Input
              id="detail-loc-name"
              name="name"
              defaultValue={node.name}
              required
              disabled={!canManage}
            />
          </div>
          
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Status"
              name="status"
              defaultValue={node.status}
              disabled={!canManage}
              options={[
                { value: LocationStatus.Active, label: "Active" },
                { value: LocationStatus.Inactive, label: "Inactive" },
              ]}
            />
          </div>

          {canManage && (
            <div>
              <Button type="submit" loading={pending}>
                Save
              </Button>
            </div>
          )}
        </form>
      </CardContent>

      {canManage && (
        <CardFooter className="justify-between">
          {canHaveChildren ? (
            <Button size="sm" variant="outline" onClick={() => onAddChild(node)}>
              Add sub-location
            </Button>
          ) : (
            <span />
          )}
          <Button size="sm" variant="destructive" onClick={() => onDelete(node)}>
            Delete
          </Button>
        </CardFooter>
      )}
    </>
  );
}
