using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Application.PurchaseReturns.Commands;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Application.PurchaseReturns;

public class PurchaseReturnCommandHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Posted receipt of 6 (line cost 5) on an approved order of 10.
    private static async Task<GoodsReceipt> SeedPostedReceiptAsync(PurchasingTestHost host)
    {
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));

        return await PurchasingSeed.PostedReceiptAsync(host, order, (order.Lines[0].Id, 6));
    }

    private static CreatePurchaseReturnCommand CreateCommand(
        long receiptId,
        long receiptLineId,
        int quantity) =>
        new(new CreatePurchaseReturnRequest
        {
            GoodsReceiptId = receiptId,
            Reason = PurchaseReturnReason.Damaged,
            Lines = [new PurchaseReturnLineRequest { GoodsReceiptLineId = receiptLineId, Quantity = quantity }],
        });

    private static CreatePurchaseReturnCommandHandler MakeCreate(PurchasingTestHost host) =>
        new(host.Context, host.DateTime, NullLogger<CreatePurchaseReturnCommandHandler>.Instance);

    private static async Task<PurchaseReturn> ReloadAsync(
        PurchasingTestHost host,
        long id)
    {
        PurchasingSeed.Detach(host);

        return await host.Context.PurchaseReturns.Include(x => x.Lines).SingleAsync(x => x.Id == id, Ct);
    }

    // Create

    [Fact]
    public async Task Create_ShouldPersistADraft_AgainstAPostedReceipt()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        PurchasingSeed.Detach(host);
        var lineId = receipt.Lines[0].Id;

        var result = await MakeCreate(host).Handle(CreateCommand(receipt.Id, lineId, 2), Ct);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, result.Data);
        Assert.Equal(PurchaseReturnStatus.Draft, stored.Status);
        Assert.Equal(2, stored.TotalQuantity);
        Assert.Equal(5m, stored.Lines[0].ReceiptUnitCostBase);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_ForAnUnknownReceipt_AndConflictForAReceiptNotPosted()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var posting = await PurchasingSeed.ReceivingAsync(host, order, null, (order.Lines[0].Id, 3));
        PurchasingSeed.Detach(host);

        Assert.False((await MakeCreate(host).Handle(CreateCommand(999, 1, 1), Ct)).IsSuccess);
        await Assert.ThrowsAsync<ConflictException>(() =>
            MakeCreate(host).Handle(CreateCommand(posting.Id, posting.Lines[0].Id, 1), Ct));
    }

    [Fact]
    public async Task Create_ShouldEnforceOverReturn_CountingOtherDraftsButNotCancelledReturns()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var lineId = receipt.Lines[0].Id;
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (lineId, 4));
        PurchasingSeed.Detach(host);

        // 6 received - 4 claimed by a draft = 2 left.
        await Assert.ThrowsAsync<ValidationException>(() => MakeCreate(host).Handle(CreateCommand(receipt.Id, lineId, 3), Ct));

        var cancel = new CancelPurchaseReturnCommandHandler(host.Context, host.DateTime);
        Assert.True((await cancel.Handle(
            new CancelPurchaseReturnCommand(draft.Id, new CancelPurchaseReturnRequest { Reason = "x" }, "u"), Ct)).IsSuccess);

        // The cancelled draft no longer claims anything.
        Assert.True((await MakeCreate(host).Handle(CreateCommand(receipt.Id, lineId, 6), Ct)).IsSuccess);
    }

    // Update draft

    [Fact]
    public async Task Update_ShouldReplaceLines_ExcludingTheEditedReturnFromTheGuard()
    {
        // Arrange — fresh context, as in production.
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var lineId = receipt.Lines[0].Id;
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (lineId, 5));
        PurchasingSeed.Detach(host);

        // Act — 5 is claimed by this very draft, so raising it to 6 must be allowed.
        var result = await new UpdatePurchaseReturnCommandHandler(host.Context).Handle(
            new UpdatePurchaseReturnCommand(
                draft.Id,
                new UpdatePurchaseReturnRequest
                {
                    Reason = PurchaseReturnReason.WrongItem,
                    Lines = [new PurchaseReturnLineRequest { GoodsReceiptLineId = lineId, Quantity = 6 }],
                }),
            Ct);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, draft.Id);
        Assert.Equal(6, stored.TotalQuantity);
        Assert.Equal(PurchaseReturnReason.WrongItem, stored.Reason);
    }

    [Fact]
    public async Task Update_ShouldCountAnotherReturnsClaim_AndRefuseANonDraft()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var lineId = receipt.Lines[0].Id;
        var mine = await PurchasingSeed.ReturnDraftAsync(host, receipt, (lineId, 2));
        await PurchasingSeed.ReturnDraftAsync(host, receipt, (lineId, 3));
        var posting = await PurchasingSeed.ReturnPostingAsync(host, receipt, host.DateTime.UtcNow, (receipt.Lines[0].Id, 1));
        PurchasingSeed.Detach(host);
        var handler = new UpdatePurchaseReturnCommandHandler(host.Context);
        UpdatePurchaseReturnCommand Command(long id, int quantity) =>
            new(id, new UpdatePurchaseReturnRequest
            {
                Reason = PurchaseReturnReason.Other,
                Lines = [new PurchaseReturnLineRequest { GoodsReceiptLineId = lineId, Quantity = quantity }],
            });

        // 6 received - 3 (other draft) - 1 (posting return) = 2 available to "mine".
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(mine.Id, 3), Ct));
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(Command(posting.Id, 1), Ct));
        Assert.False((await handler.Handle(Command(999, 1), Ct)).IsSuccess);
    }

    // Post

    private static PostPurchaseReturnCommandHandler MakePost(
        PurchasingTestHost host,
        Mock<StarterKit.Inventory.Contracts.Services.IInventoryService> inventory) =>
        new(host.Context, inventory.Object, host.DateTime, NullLogger<PostPurchaseReturnCommandHandler>.Instance);

    private static void SetupIssue(
        Mock<StarterKit.Inventory.Contracts.Services.IInventoryService> inventory,
        decimal unitCost) =>
        inventory.SetupIssueAtCost(unitCost);

    [Fact]
    public async Task Post_ShouldIssueStock_RecordCostRemoved_AndMoveToPosted()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (receipt.Lines[0].Id, 2));
        PurchasingSeed.Detach(host);
        var inventory = InventoryMock.Create();
        SetupIssue(inventory, 7m);

        var result = await MakePost(host, inventory).Handle(new PostPurchaseReturnCommand(draft.Id, "poster"), Ct);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, draft.Id);
        Assert.Equal(PurchaseReturnStatus.Posted, stored.Status);
        Assert.Equal("poster", stored.PostedBy);
        Assert.Equal(14m, stored.CostRemovedBase);
        Assert.Equal(10m, stored.ExpectedCreditBase);
    }

    [Fact]
    public async Task Post_ShouldRethrowInsufficientStock_AndReturnToDraft()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (receipt.Lines[0].Id, 2));
        PurchasingSeed.Detach(host);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new InsufficientStockException("Insufficient stock"));

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            MakePost(host, inventory).Handle(new PostPurchaseReturnCommand(draft.Id, "poster"), Ct));

        Assert.Equal(PurchaseReturnStatus.Draft, (await ReloadAsync(host, draft.Id)).Status);
    }

    [Fact]
    public async Task Post_ShouldRethrowATransientConflict_AndStayPosting()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (receipt.Lines[0].Id, 2));
        PurchasingSeed.Detach(host);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            MakePost(host, inventory).Handle(new PostPurchaseReturnCommand(draft.Id, "poster"), Ct));

        Assert.Equal(PurchaseReturnStatus.Posting, (await ReloadAsync(host, draft.Id)).Status);
    }

    [Fact]
    public async Task Post_ShouldGuardNotFoundNonDraftAndEmptyReturns()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var posting = await PurchasingSeed.ReturnPostingAsync(host, receipt, host.DateTime.UtcNow, (receipt.Lines[0].Id, 1));
        PurchasingSeed.Detach(host);
        var handler = MakePost(host, InventoryMock.Create());

        Assert.False((await handler.Handle(new PostPurchaseReturnCommand(999, "u"), Ct)).IsSuccess);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new PostPurchaseReturnCommand(posting.Id, "u"), Ct));
    }

    // Credit / cancel

    [Fact]
    public async Task MarkCredited_ShouldRequireAPostedReturn_AndRecordTheCreditNote()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (receipt.Lines[0].Id, 2));
        var handler = new MarkPurchaseReturnCreditedCommandHandler(host.Context, host.DateTime);
        var model = new MarkPurchaseReturnCreditedRequest { CreditNoteNumber = "  CN-1 ", CreditAmount = 9.5m };

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new MarkPurchaseReturnCreditedCommand(draft.Id, model, "acct"), Ct));

        draft.BeginPost("poster", host.DateTime.UtcNow);
        draft.CompletePost(draft.Lines.ToDictionary(x => x.Id, _ => 7m), host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(Ct);

        Assert.True((await handler.Handle(new MarkPurchaseReturnCreditedCommand(draft.Id, model, "acct"), Ct)).IsSuccess);
        var stored = await ReloadAsync(host, draft.Id);
        Assert.Equal(PurchaseReturnStatus.Credited, stored.Status);
        Assert.Equal("CN-1", stored.CreditNoteNumber);
        Assert.Equal(9.5m, stored.CreditAmountBase);
        Assert.Equal("acct", stored.CreditedBy);
        Assert.False((await handler.Handle(new MarkPurchaseReturnCreditedCommand(999, model, "u"), Ct)).IsSuccess);
    }

    [Fact]
    public async Task Cancel_ShouldOnlyWorkForADraft_AndRecordTheActor()
    {
        using var host = new PurchasingTestHost();
        var receipt = await SeedPostedReceiptAsync(host);
        var draft = await PurchasingSeed.ReturnDraftAsync(host, receipt, (receipt.Lines[0].Id, 1));
        var posting = await PurchasingSeed.ReturnPostingAsync(host, receipt, host.DateTime.UtcNow, (receipt.Lines[0].Id, 1));
        var handler = new CancelPurchaseReturnCommandHandler(host.Context, host.DateTime);
        var model = new CancelPurchaseReturnRequest { Reason = "mistake" };

        Assert.True((await handler.Handle(new CancelPurchaseReturnCommand(draft.Id, model, "u1"), Ct)).IsSuccess);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new CancelPurchaseReturnCommand(posting.Id, model, "u1"), Ct));

        var stored = await ReloadAsync(host, draft.Id);
        Assert.Equal(PurchaseReturnStatus.Cancelled, stored.Status);
        Assert.Equal("u1", stored.CancelledBy);
    }
}
