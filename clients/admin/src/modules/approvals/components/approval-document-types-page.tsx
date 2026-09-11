import { Card, CardContent } from "@/components/ui/card";
import { getApprovalDocumentTypes } from "@/modules/approvals/api/document-types.api";
import { DocumentTypesDataTable } from "@/modules/approvals/components/document-types-data-table";
import { APPROVAL_DOCUMENT_TYPES_PERMISSIONS } from "@/modules/approvals/constants/document-type-permissions";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";

export async function ApprovalDocumentTypesPage() {
  const { session, denied } = await requirePermission(APPROVAL_DOCUMENT_TYPES_PERMISSIONS.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, APPROVAL_DOCUMENT_TYPES_PERMISSIONS.Create);
  const canUpdate = hasPermission(session, APPROVAL_DOCUMENT_TYPES_PERMISSIONS.Update);
  const canDelete = hasPermission(session, APPROVAL_DOCUMENT_TYPES_PERMISSIONS.Delete);

  const result = await getApprovalDocumentTypes();

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load document types", description: result.message || "Please try again." }
      : undefined;
  const documentTypes = result.data ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Document types</h1>
        <p className="text-sm text-muted-foreground">
          Categorize approval requests by the kind of document they concern.
        </p>
      </div>

      <Card>
        <CardContent>
          <DocumentTypesDataTable
            documentTypes={documentTypes}
            error={error}
            canCreate={canCreate}
            canUpdate={canUpdate}
            canDelete={canDelete}
          />
        </CardContent>
      </Card>
    </div>
  );
}
