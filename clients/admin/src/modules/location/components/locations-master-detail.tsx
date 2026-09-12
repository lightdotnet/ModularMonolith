"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { CreateLocationDialog } from "@/modules/location/components/create-location-dialog";
import { DeleteLocationDialog } from "@/modules/location/components/delete-location-dialog";
import { LocationDetailPanel } from "@/modules/location/components/location-detail-panel";
import { LocationTree } from "@/modules/location/components/location-tree";
import { MoveLocationDialog } from "@/modules/location/components/move-location-dialog";
import { flattenLocationTree, type LocationTreeNodeDto } from "@/modules/location/types/location";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

interface LocationsMasterDetailProps {
  nodes: LocationTreeNodeDto[];
  locationTypes: LocationTypeDto[];
  error?: string;
  canManage?: boolean;
}

type DialogAction = "create" | "delete" | "move";

export function LocationsMasterDetail({
  nodes,
  locationTypes,
  error,
  canManage,
}: LocationsMasterDetailProps) {
  const router = useRouter();
  const [selectedId, setSelectedId] = useState<string | undefined>(undefined);
  const [dialog, setDialog] = useState<DialogAction | null>(null);
  const [dialogKey, setDialogKey] = useState(0);
  const [selectedNode, setSelectedNode] = useState<LocationTreeNodeDto | null>(null);
  const [newLocParent, setNewLocParent] = useState<LocationTreeNodeDto | null>(null);

  const flatNodes = flattenLocationTree(nodes);

  // Re-sync selection against the latest `nodes` prop during render (rather than in an
  // effect) — the recommended React pattern for adjusting state when a prop changes:
  // https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes
  const [prevNodes, setPrevNodes] = useState(nodes);
  if (nodes !== prevNodes) {
    setPrevNodes(nodes);
    if (selectedId && !flatNodes.some((node) => node.id === selectedId)) {
      setSelectedId(undefined);
    }
  }

  const selectedDetailNode = selectedId
    ? (flatNodes.find((node) => node.id === selectedId) ?? null)
    : null;

  function openCreate(parent: LocationTreeNodeDto | null) {
    setNewLocParent(parent);
    setDialogKey((key) => key + 1);
    setDialog("create");
  }

  function openDelete(node: LocationTreeNodeDto) {
    setSelectedNode(node);
    setDialog("delete");
  }

  function openMove(node: LocationTreeNodeDto) {
    setSelectedNode(node);
    setDialogKey((key) => key + 1);
    setDialog("move");
  }

  function refresh() {
    router.refresh();
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1fr_360px]">
      <Card>
        {canManage && (
          <CardHeader>
            <Button size="sm" onClick={() => openCreate(null)}>
              <Plus />
              Add top-level location
            </Button>
          </CardHeader>
        )}
        <CardContent>
          <LocationTree
            nodes={nodes}
            locationTypes={locationTypes}
            error={error}
            canManage={canManage}
            selectedId={selectedId}
            onSelect={(node) => setSelectedId(node.id)}
            onAddChild={openCreate}
            onMove={openMove}
            onDelete={openDelete}
          />
        </CardContent>
      </Card>

      <Card>
        <LocationDetailPanel
          node={selectedDetailNode}
          nodes={nodes}
          locationTypes={locationTypes}
          canManage={canManage}
          onMove={openMove}
          onDelete={openDelete}
          onAddChild={openCreate}
          onUpdated={refresh}
        />
      </Card>

      <CreateLocationDialog
        key={`create-${dialogKey}`}
        open={dialog === "create"}
        onOpenChange={(open) => setDialog(open ? "create" : null)}
        parent={newLocParent}
        locationTypes={locationTypes}
        onCreated={refresh}
      />
      <DeleteLocationDialog
        open={dialog === "delete"}
        onOpenChange={(open) => setDialog(open ? "delete" : null)}
        node={selectedNode}
        onDeleted={refresh}
      />
      <MoveLocationDialog
        key={`move-${dialogKey}`}
        open={dialog === "move"}
        onOpenChange={(open) => setDialog(open ? "move" : null)}
        node={selectedNode}
        onMoved={refresh}
      />
    </div>
  );
}
