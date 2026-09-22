namespace StarterKit.Transfers.Contracts.Authorization;

public static class TransfersPermissions
{
    public const string Group = "transfers";

    public static class Transfers
    {
        public const string View = $"{Group}.transfers.view";

        /// <summary>Create a draft transfer, edit its header and lines, and cancel it while still a draft.</summary>
        public const string Create = $"{Group}.transfers.create";

        public const string Dispatch = $"{Group}.transfers.dispatch";

        public const string Receive = $"{Group}.transfers.receive";

        public const string Close = $"{Group}.transfers.close";
    }
}
