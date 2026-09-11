namespace StarterKit.Approval.Contracts.Authorization;

public static class ApprovalPermissions
{
    public const string Group = "approval";

    public static class Requests
    {
        public const string ViewAll = $"{Group}.requests.view_all";
    }

    public static class DocumentTypes
    {
        public const string View = $"{Group}.document_types.view";

        public const string Create = $"{Group}.document_types.create";

        public const string Update = $"{Group}.document_types.update";

        public const string Delete = $"{Group}.document_types.delete";
    }
}
