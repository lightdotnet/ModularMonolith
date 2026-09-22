"use client";

import { Combobox } from "@/components/ui/combobox";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface LocationSelectProps {
  id?: string;
  name?: string;
  value: string;
  onValueChange: (value: string) => void;
  locations: LocationTreeNodeDto[];
  placeholder?: string;
}

/** Flat location picker fed by the already-flattened location tree — used by the create/edit transfer forms. */
export function LocationSelect({
  id,
  name,
  value,
  onValueChange,
  locations,
  placeholder = "Select a location",
}: LocationSelectProps) {
  return (
    <Combobox
      id={id}
      name={name}
      value={value || null}
      onValueChange={onValueChange}
      placeholder={placeholder}
      options={locations.map((location) => ({ value: location.id, label: location.name }))}
    />
  );
}
