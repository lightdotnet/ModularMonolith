namespace StarterKit.Orders.Contracts.Orders;

public class OrderFeeDto : BaseDto<long>
{
    public string OrderCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Amount { get; set; }

    public string FeeTypeId { get; set; } = null!;

    public string FeeTypeName { get; set; } = null!;
}
