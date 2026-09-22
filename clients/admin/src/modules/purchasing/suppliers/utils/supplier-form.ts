import { SUPPLIER_LIMITS, type CreateSupplierRequest } from "../types/supplier";

function text(formData: FormData, key: string): string {
  return String(formData.get(key) ?? "").trim();
}

/** Reads and validates the supplier form (limits mirror the backend validators). Not a Server Action file — plain helper. */
export function readSupplierForm(
  formData: FormData,
): { request: CreateSupplierRequest } | { error: string } {
  const code = text(formData, "code");
  const name = text(formData, "name");
  const contactName = text(formData, "contactName");
  const phone = text(formData, "phone");
  const email = text(formData, "email");
  const address = text(formData, "address");
  const paymentTerms = text(formData, "paymentTerms");

  if (!code) return { error: "Code is required." };
  if (!name) return { error: "Name is required." };

  const checks: [string, string, number][] = [
    ["Code", code, SUPPLIER_LIMITS.code],
    ["Name", name, SUPPLIER_LIMITS.name],
    ["Contact name", contactName, SUPPLIER_LIMITS.contactName],
    ["Phone", phone, SUPPLIER_LIMITS.phone],
    ["Email", email, SUPPLIER_LIMITS.email],
    ["Address", address, SUPPLIER_LIMITS.address],
    ["Payment terms", paymentTerms, SUPPLIER_LIMITS.paymentTerms],
  ];
  for (const [label, value, max] of checks) {
    if (value.length > max) return { error: `${label} must not exceed ${max} characters.` };
  }

  return {
    request: {
      code,
      name,
      contactName: contactName || undefined,
      phone: phone || undefined,
      email: email || undefined,
      address: address || undefined,
      paymentTerms: paymentTerms || undefined,
    },
  };
}
