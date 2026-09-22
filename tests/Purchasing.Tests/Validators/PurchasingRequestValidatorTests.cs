using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;
using StarterKit.Shared;

namespace Purchasing.Tests.Validators;

public class PurchasingRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static FakeDateTime Clock() => new() { UtcNow = Now };

    private static AddPurchaseOrderLineRequest Line(
        int quantity = 1,
        decimal unitCost = 1m) =>
        new() { ProductId = 1, Quantity = quantity, UnitCost = unitCost };

    [Theory]
    [InlineData(1, true)]
    [InlineData(1_000_000, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1_000_001, false)]
    public void AddLine_ShouldBoundTheQuantity(int quantity, bool expected)
    {
        Assert.Equal(expected, new AddPurchaseOrderLineRequestValidator().Validate(Line(quantity: quantity)).IsValid);
        Assert.Equal(expected, new UpdatePurchaseOrderLineRequestValidator()
            .Validate(new UpdatePurchaseOrderLineRequest { Quantity = quantity, UnitCost = 1m }).IsValid);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("1000000000", true)]
    [InlineData("1000000000.0001", false)]
    [InlineData("-0.0001", false)]
    [InlineData("12.34567", false)]
    [InlineData("12.3456", true)]
    public void UnitCost_ShouldBeBetweenZeroAndOneBillion_WithAtMostFourDecimals(
        string cost,
        bool expected)
    {
        var amount = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, new AddPurchaseOrderLineRequestValidator().Validate(Line(unitCost: amount)).IsValid);
        Assert.Equal(expected, new UpdatePurchaseOrderLineRequestValidator()
            .Validate(new UpdatePurchaseOrderLineRequest { Quantity = 1, UnitCost = amount }).IsValid);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("1000000000", true)]
    [InlineData("1000000000.01", false)]
    [InlineData("-1", false)]
    [InlineData("1.00001", false)]
    public void CreditAmount_ShouldBeBoundedAndHaveAtMostFourDecimals(
        string amount,
        bool expected)
    {
        var request = new MarkPurchaseReturnCreditedRequest
        {
            CreditNoteNumber = "CN-1",
            CreditAmount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
        };

        Assert.Equal(expected, new MarkPurchaseReturnCreditedRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void CreditNote_ShouldBeRequiredAndCappedAt100Characters()
    {
        var validator = new MarkPurchaseReturnCreditedRequestValidator();

        Assert.False(validator.Validate(new MarkPurchaseReturnCreditedRequest { CreditNoteNumber = "", CreditAmount = 1m }).IsValid);
        Assert.False(validator.Validate(new MarkPurchaseReturnCreditedRequest { CreditNoteNumber = new string('x', 101), CreditAmount = 1m }).IsValid);
        Assert.True(validator.Validate(new MarkPurchaseReturnCreditedRequest { CreditNoteNumber = new string('x', 100), CreditAmount = 1m }).IsValid);
    }

    private static ReceivePurchaseOrderRequest Receive(params (long LineId, int Quantity)[] lines) =>
        new()
        {
            ReceivedAt = Now,
            Lines = lines.Select(x => new ReceivePurchaseOrderLineRequest { PurchaseOrderLineId = x.LineId, Quantity = x.Quantity }).ToList(),
        };

    [Fact]
    public void Receive_ShouldBoundQuantities_LineCount_AndRejectDuplicateLines()
    {
        var validator = new ReceivePurchaseOrderRequestValidator();

        Assert.True(validator.Validate(Receive((1, 1_000_000))).IsValid);
        Assert.False(validator.Validate(Receive((1, 1_000_001))).IsValid);
        Assert.False(validator.Validate(Receive((1, 0))).IsValid);
        Assert.False(validator.Validate(Receive()).IsValid);
        Assert.False(validator.Validate(Receive((1, 1), (1, 2))).IsValid);
        Assert.True(validator.Validate(Receive(Enumerable.Range(1, 200).Select(x => ((long)x, 1)).ToArray())).IsValid);
        Assert.False(validator.Validate(Receive(Enumerable.Range(1, 201).Select(x => ((long)x, 1)).ToArray())).IsValid);
        Assert.False(validator.Validate(Receive((0, 1))).IsValid);
    }

    [Fact]
    public void Receive_ShouldCapTheDeliveryNoteAt100Characters()
    {
        var validator = new ReceivePurchaseOrderRequestValidator();
        var request = Receive((1, 1));

        Assert.True(validator.Validate(request with { DeliveryNoteRef = new string('x', 100) }).IsValid);
        Assert.False(validator.Validate(request with { DeliveryNoteRef = new string('x', 101) }).IsValid);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(-1000, true)]
    public void ReceiveCommand_ShouldRejectAReceivedDateMoreThanFiveMinutesInTheFuture(
        int minutesFromNow,
        bool expected)
    {
        var validator = new ReceivePurchaseOrderCommandValidator(Clock());
        var command = new ReceivePurchaseOrderCommand(
            1,
            Receive((1, 1)) with { ReceivedAt = Now.AddMinutes(minutesFromNow) },
            "user");

        Assert.Equal(expected, validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(-2, false)]
    [InlineData(30, true)]
    public void CreateCommand_ShouldRejectAnExpectedDateMoreThanADayInThePast(
        int daysFromNow,
        bool expected)
    {
        var validator = new CreatePurchaseOrderCommandValidator(Clock());
        var command = new CreatePurchaseOrderCommand(
            new CreatePurchaseOrderRequest { SupplierId = 1, LocationId = "loc", ExpectedAt = Now.AddDays(daysFromNow) },
            "u",
            "e");

        Assert.Equal(expected, validator.Validate(command).IsValid);
        Assert.True(validator.Validate(command with { Model = command.Model with { ExpectedAt = null } }).IsValid);
    }

    [Fact]
    public void CreatePurchaseReturn_ShouldBoundQuantities_LineCount_ReasonAndDuplicates()
    {
        var validator = new CreatePurchaseReturnRequestValidator();
        CreatePurchaseReturnRequest Request(params (long LineId, int Quantity)[] lines) =>
            new()
            {
                GoodsReceiptId = 1,
                Reason = PurchaseReturnReason.Defective,
                Lines = lines.Select(x => new PurchaseReturnLineRequest { GoodsReceiptLineId = x.LineId, Quantity = x.Quantity }).ToList(),
            };

        Assert.True(validator.Validate(Request((1, 1_000_000))).IsValid);
        Assert.False(validator.Validate(Request((1, 1_000_001))).IsValid);
        Assert.False(validator.Validate(Request((1, 0))).IsValid);
        Assert.False(validator.Validate(Request()).IsValid);
        Assert.False(validator.Validate(Request((1, 1), (1, 1))).IsValid);
        Assert.True(validator.Validate(Request(Enumerable.Range(1, 200).Select(x => ((long)x, 1)).ToArray())).IsValid);
        Assert.False(validator.Validate(Request(Enumerable.Range(1, 201).Select(x => ((long)x, 1)).ToArray())).IsValid);
        Assert.False(validator.Validate(Request((1, 1)) with { Reason = (PurchaseReturnReason)99 }).IsValid);
        Assert.False(validator.Validate(Request((1, 1)) with { GoodsReceiptId = 0 }).IsValid);
    }

    [Fact]
    public void Supplier_ShouldValidateRequiredFieldsAndLengths()
    {
        var validator = new CreateSupplierRequestValidator();

        Assert.True(validator.Validate(new CreateSupplierRequest { Code = "C", Name = "N" }).IsValid);
        Assert.False(validator.Validate(new CreateSupplierRequest { Code = "", Name = "N" }).IsValid);
        Assert.False(validator.Validate(new CreateSupplierRequest { Code = new string('x', 51), Name = "N" }).IsValid);
        Assert.False(validator.Validate(new CreateSupplierRequest { Code = "C", Name = new string('x', 201) }).IsValid);
        Assert.False(validator.Validate(new CreateSupplierRequest { Code = "C", Name = "N", Email = new string('x', 257) }).IsValid);
    }

    [Fact]
    public void CommandValidators_ShouldRequirePositiveIdsAndUsers()
    {
        Assert.False(new SubmitPurchaseOrderCommandValidator().Validate(
            new SubmitPurchaseOrderCommand(0, new SubmitPurchaseOrderRequest { ApproverEmployeeId = "e" }, "u")).IsValid);
        Assert.False(new SubmitPurchaseOrderCommandValidator().Validate(
            new SubmitPurchaseOrderCommand(1, new SubmitPurchaseOrderRequest { ApproverEmployeeId = "e" }, "")).IsValid);
        Assert.False(new WithdrawPurchaseOrderCommandValidator().Validate(new WithdrawPurchaseOrderCommand(0, "u")).IsValid);
        Assert.True(new WithdrawPurchaseOrderCommandValidator().Validate(new WithdrawPurchaseOrderCommand(1, "u")).IsValid);
    }
}
