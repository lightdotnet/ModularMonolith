import type { ComboboxOption } from "@/components/ui/combobox";

// Local accounts are represented by an empty AuthProvider on the backend
// (User.ChangeAuthProvider treats "" the same as null) — only "AD" is a real value.
export const AUTH_PROVIDER_SELECT_OPTIONS: ComboboxOption[] = [
  { value: "", label: "--" },
  { value: "AD", label: "AD" },
];
