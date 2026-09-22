namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record SubmitPurchaseOrderRequest
{
    /// <summary>One of the requester approver candidates (see the approvers endpoint).</summary>
    public string ApproverEmployeeId { get; set; } = null!;
}

public sealed class SubmitPurchaseOrderRequestValidator : AbstractValidator<SubmitPurchaseOrderRequest>
{
    public SubmitPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.ApproverEmployeeId).NotEmpty().MaximumLength(450);
    }
}
