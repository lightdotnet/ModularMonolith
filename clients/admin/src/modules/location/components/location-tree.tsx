"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight, MapPin, Pencil } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty";
import { cn } from "@/lib/shared/utils";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

interface LocationTreeProps {
  nodes: LocationTreeNodeDto[];
  locationTypes: LocationTypeDto[];
  error?: string;
  canManage?: boolean;
  selectedId?: string;
  onSelect: (node: LocationTreeNodeDto) => void;
  onAddChild: (parent: LocationTreeNodeDto) => void;
  onMove: (node: LocationTreeNodeDto) => void;
  onDelete: (node: LocationTreeNodeDto) => void;
}

export function LocationTree({
  nodes,
  locationTypes,
  error,
  canManage,
  selectedId,
  onSelect,
  onAddChild,
  onMove,
  onDelete,
}: LocationTreeProps) {
  return (
    <div className="flex flex-col gap-3">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {nodes.length === 0 ? (
        <Empty>
          <EmptyTitle>No locations yet</EmptyTitle>
          <EmptyDescription>Add a top-level location to get started.</EmptyDescription>
        </Empty>
      ) : (
        <div className="flex flex-col gap-1 rounded-md border border-border p-2">
          {nodes.map((node) => (
            <LocationTreeNode
              key={node.id}
              node={node}
              locationTypes={locationTypes}
              canManage={canManage}
              selectedId={selectedId}
              onSelect={onSelect}
              onAddChild={onAddChild}
              onMove={onMove}
              onDelete={onDelete}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface LocationTreeNodeProps {
  node: LocationTreeNodeDto;
  locationTypes: LocationTypeDto[];
  canManage?: boolean;
  selectedId?: string;
  onSelect: (node: LocationTreeNodeDto) => void;
  onAddChild: (parent: LocationTreeNodeDto) => void;
  onMove: (node: LocationTreeNodeDto) => void;
  onDelete: (node: LocationTreeNodeDto) => void;
}

function LocationTreeNode({
  node,
  locationTypes,
  canManage,
  selectedId,
  onSelect,
  onAddChild,
  onMove,
  onDelete,
}: LocationTreeNodeProps) {
  const [expanded, setExpanded] = useState(true);
  const hasChildren = node.children.length > 0;
  const isSelected = node.id === selectedId;

  const typeDto = locationTypes.find((t) => t.id === node.locationTypeId);
  const typeName = typeDto?.name ?? node.locationTypeId;
  const canHaveChildren = typeDto?.canHaveChildren ?? false;

  return (
    <div>
      <div
        className={cn(
          "flex cursor-pointer items-center gap-1.5 rounded-md py-2 pl-1 pr-2",
          isSelected ? "bg-accent" : "hover:bg-accent/50",
        )}
        onClick={() => onSelect(node)}
      >
        <button
          type="button"
          aria-label={expanded ? "Collapse" : "Expand"}
          onClick={(event) => {
            event.stopPropagation();
            setExpanded((value) => !value);
          }}
          className="flex size-5 shrink-0 items-center justify-center text-muted-foreground"
          disabled={!hasChildren}
        >
          {hasChildren ? (
            expanded ? (
              <ChevronDown className="size-4" />
            ) : (
              <ChevronRight className="size-4" />
            )
          ) : null}
        </button>

        <Badge variant="outline" className="rounded bg-muted/50 text-muted-foreground font-normal">{typeName}</Badge>
        <span className="font-mono text-xs text-muted-foreground">{node.code}</span>
        <span className="text-muted-foreground">|</span>
        <span>{node.name}</span>
        {node.status !== "Active" && <Badge variant="secondary">{node.status}</Badge>}

        <div className="ml-auto flex items-center gap-1">
          {canManage && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  aria-label="Row actions"
                  size="icon-xs"
                  variant="outline"
                  onClick={(event) => event.stopPropagation()}
                >
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canHaveChildren && (
                  <DropdownMenuItem onClick={() => onAddChild(node)}>
                    Add sub-location
                  </DropdownMenuItem>
                )}
                <DropdownMenuItem onClick={() => onMove(node)}>Move</DropdownMenuItem>
                <DropdownMenuItem variant="destructive" onClick={() => onDelete(node)}>
                  Delete
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {hasChildren && expanded && (
        <div className="ml-2.5 border-l border-border pl-4">
          {node.children.map((child) => (
            <LocationTreeNode
              key={child.id}
              node={child}
              locationTypes={locationTypes}
              canManage={canManage}
              selectedId={selectedId}
              onSelect={onSelect}
              onAddChild={onAddChild}
              onMove={onMove}
              onDelete={onDelete}
            />
          ))}
        </div>
      )}
    </div>
  );
}
