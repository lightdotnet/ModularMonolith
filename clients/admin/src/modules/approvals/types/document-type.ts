/** Mirrors `Approval.Contracts/DocumentTypes/ApprovalDocumentTypeDto.cs` — keep in sync with the backend. */
export interface ApprovalDocumentTypeDto {
  id: string;
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
  created: string;
}

export interface CreateApprovalDocumentTypeRequest {
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
}
