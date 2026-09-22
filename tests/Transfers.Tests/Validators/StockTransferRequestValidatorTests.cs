using StarterKit.Transfers.Contracts.StockTransfers;

namespace Transfers.Tests.Validators;

public class StockTransferRequestValidatorTests
{
    private static ReceiveStockTransferRequest Receive(
        string clientRequestId = "req-1",
        params (long LineId, int Quantity)[] lines) =>
        new()
        {
            ClientRequestId = clientRequestId,
            Lines = (lines.Length == 0 ? [(1L, 1)] : lines)
                .Select(x => new ReceiveStockTransferLineRequest { TransferLineId = x.LineId, Quantity = x.Quantity })
                .ToList(),
        };

    [Theory]
    [InlineData(1, true)]
    [InlineData(1_000_000, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1_000_001, false)]
    public void AddLine_ShouldBoundTheQuantity(
        int quantity,
        bool expected)
    {
        var result = new AddStockTransferLineRequestValidator()
            .Validate(new AddStockTransferLineRequest { ProductId = 1, Quantity = quantity });

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void AddLine_ShouldRequireAPositiveProductId()
    {
        Assert.False(new AddStockTransferLineRequestValidator()
            .Validate(new AddStockTransferLineRequest { ProductId = 0, Quantity = 1 }).IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1_000_000, true)]
    [InlineData(0, false)]
    [InlineData(1_000_001, false)]
    public void UpdateLine_ShouldBoundTheQuantity(
        int quantity,
        bool expected)
    {
        var result = new UpdateStockTransferLineRequestValidator()
            .Validate(new UpdateStockTransferLineRequest { Quantity = quantity });

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1_000_000, true)]
    [InlineData(0, false)]
    [InlineData(1_000_001, false)]
    public void Receive_ShouldBoundEachLineQuantity(
        int quantity,
        bool expected)
    {
        var result = new ReceiveStockTransferRequestValidator().Validate(Receive("r", (1, quantity)));

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void Receive_ShouldAllowAtMost200Lines()
    {
        var validator = new ReceiveStockTransferRequestValidator();
        var atLimit = Receive("r", Enumerable.Range(1, 200).Select(x => ((long)x, 1)).ToArray());
        var overLimit = Receive("r", Enumerable.Range(1, 201).Select(x => ((long)x, 1)).ToArray());

        Assert.True(validator.Validate(atLimit).IsValid);
        Assert.False(validator.Validate(overLimit).IsValid);
    }

    [Fact]
    public void Receive_ShouldRejectEmptyLines_DuplicateLines_AndBadIds()
    {
        var validator = new ReceiveStockTransferRequestValidator();

        Assert.False(validator.Validate(new ReceiveStockTransferRequest { ClientRequestId = "r", Lines = [] }).IsValid);
        Assert.False(validator.Validate(Receive("r", (1, 1), (1, 2))).IsValid);
        Assert.False(validator.Validate(Receive("r", (0, 1))).IsValid);
        Assert.False(validator.Validate(Receive("", (1, 1))).IsValid);
        Assert.False(validator.Validate(Receive(new string('x', 101), (1, 1))).IsValid);
        Assert.True(validator.Validate(Receive(new string('x', 100), (1, 1))).IsValid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("r", true)]
    public void CloseAndCancel_ShouldRequireAReason(
        string reason,
        bool expected)
    {
        Assert.Equal(expected, new CloseStockTransferRequestValidator().Validate(new CloseStockTransferRequest { Reason = reason }).IsValid);
        Assert.Equal(expected, new CancelStockTransferRequestValidator().Validate(new CancelStockTransferRequest { Reason = reason }).IsValid);
    }

    [Fact]
    public void CloseAndCancel_ShouldCapTheReasonAt1000Characters()
    {
        Assert.True(new CloseStockTransferRequestValidator().Validate(new CloseStockTransferRequest { Reason = new string('x', 1000) }).IsValid);
        Assert.False(new CloseStockTransferRequestValidator().Validate(new CloseStockTransferRequest { Reason = new string('x', 1001) }).IsValid);
        Assert.False(new CancelStockTransferRequestValidator().Validate(new CancelStockTransferRequest { Reason = new string('x', 1001) }).IsValid);
    }

    [Fact]
    public void CreateAndUpdate_ShouldValidateLocationIdsAndNote()
    {
        var create = new CreateStockTransferRequestValidator();
        var update = new UpdateStockTransferRequestValidator();

        Assert.True(create.Validate(new CreateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "b" }).IsValid);
        Assert.False(create.Validate(new CreateStockTransferRequest { SourceLocationId = "", DestinationLocationId = "b" }).IsValid);
        Assert.False(create.Validate(new CreateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = new string('x', 451) }).IsValid);
        Assert.False(create.Validate(new CreateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "b", Note = new string('x', 1001) }).IsValid);
        Assert.True(update.Validate(new UpdateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "b", Note = new string('x', 1000) }).IsValid);
        Assert.False(update.Validate(new UpdateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "" }).IsValid);
    }
}
