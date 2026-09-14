using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Orders;

public class OrderFeeDto : BaseDto<long>
{
    public string OrderCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Amount { get; set; }

    public OrderFeeType Type { get; set; }
}
