namespace StarterKit.Orders.Contracts.Common;

public enum OrderStatus
{
    Draft = 0,

    Placed = 1,

    PartiallyPaid = 2,

    Paid = 3,

    Fulfilled = 4,

    Cancelled = 5,
}
