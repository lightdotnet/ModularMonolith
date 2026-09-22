using Microsoft.EntityFrameworkCore;
using Purchasing.Tests.TestSupport;
using StarterKit.Purchasing.Api.Application.PurchaseReturns.Queries;
using StarterKit.Purchasing.Api.Application.Suppliers.Commands;
using StarterKit.Purchasing.Api.Application.Suppliers.Queries;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;

namespace Purchasing.Tests.Application;

public class SupplierAndQueryHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static CreateSupplierRequest NewSupplier(string code) =>
        new() { Code = code, Name = "Acme", PaymentTerms = "Net 30" };

    // Suppliers

    [Fact]
    public async Task CreateSupplier_ShouldNormalizeTheCode_AndRejectADuplicateCaseInsensitively()
    {
        using var host = new PurchasingTestHost();
        var handler = new CreateSupplierCommandHandler(host.Context);

        var first = await handler.Handle(new CreateSupplierCommand(NewSupplier("  ab-1 ")), Ct);
        var duplicate = await handler.Handle(new CreateSupplierCommand(NewSupplier("AB-1")), Ct);

        Assert.True(first.IsSuccess);
        Assert.False(duplicate.IsSuccess);
        Assert.Contains("already exists", duplicate.Message);
        Assert.Equal("AB-1", (await host.Context.Suppliers.SingleAsync(Ct)).Code);
    }

    [Fact]
    public async Task UpdateSupplier_ShouldRejectACodeOwnedByAnotherSupplier_ButAllowKeepingItsOwn()
    {
        using var host = new PurchasingTestHost();
        var a = await PurchasingSeed.SupplierAsync(host, "A");
        await PurchasingSeed.SupplierAsync(host, "B");
        var handler = new UpdateSupplierCommandHandler(host.Context);
        UpdateSupplierCommand Command(string code) =>
            new(a.Id, new UpdateSupplierRequest { Code = code, Name = "Renamed" });

        Assert.False((await handler.Handle(Command("b"), Ct)).IsSuccess);
        Assert.True((await handler.Handle(Command("a"), Ct)).IsSuccess);
        Assert.False((await handler.Handle(new UpdateSupplierCommand(999, new UpdateSupplierRequest { Code = "z", Name = "n" }), Ct)).IsSuccess);
        Assert.Equal("Renamed", (await host.Context.Suppliers.SingleAsync(x => x.Id == a.Id, Ct)).Name);
    }

    [Fact]
    public async Task ChangeSupplierStatus_ShouldActivateAndDeactivate()
    {
        using var host = new PurchasingTestHost();
        var supplier = await PurchasingSeed.SupplierAsync(host);
        var handler = new ChangeSupplierStatusCommandHandler(host.Context);

        await handler.Handle(new ChangeSupplierStatusCommand(supplier.Id, Activate: false), Ct);
        Assert.False((await host.Context.Suppliers.SingleAsync(Ct)).IsActive);

        await handler.Handle(new ChangeSupplierStatusCommand(supplier.Id, Activate: true), Ct);
        Assert.True((await host.Context.Suppliers.SingleAsync(Ct)).IsActive);
        Assert.False((await handler.Handle(new ChangeSupplierStatusCommand(999, true), Ct)).IsSuccess);
    }

    [Fact]
    public async Task GetAndSearchSuppliers_ShouldReturnTheStoredSupplier()
    {
        using var host = new PurchasingTestHost();
        var supplier = await PurchasingSeed.SupplierAsync(host, "FIND");

        var get = await new GetSupplierByIdQueryHandler(host.Context).Handle(new GetSupplierByIdQuery(supplier.Id), Ct);
        var missing = await new GetSupplierByIdQueryHandler(host.Context).Handle(new GetSupplierByIdQuery(999), Ct);
        var search = await new SearchSuppliersQueryHandler(host.Context).Handle(new SearchSuppliersQuery(new SearchSupplierRequest()), Ct);

        Assert.Equal("FIND", get.Data.Code);
        Assert.False(missing.IsSuccess);
        Assert.Single(search.Data.Records);
    }

    // Cost masking (Inventory moving-average cost) on purchase returns

    private static async Task<long> SeedPostedReturnAsync(PurchasingTestHost host)
    {
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var receipt = await PurchasingSeed.PostedReceiptAsync(host, order, (order.Lines[0].Id, 6));
        var purchaseReturn = await PurchasingSeed.ReturnPostingAsync(host, receipt, host.DateTime.UtcNow, (receipt.Lines[0].Id, 2));

        purchaseReturn.CompletePost(purchaseReturn.Lines.ToDictionary(x => x.Id, _ => 14m), host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(Ct);
        PurchasingSeed.Detach(host);

        return purchaseReturn.Id;
    }

    [Fact]
    public async Task GetReturn_ShouldMaskCostRemoved_WithoutViewCost_ButKeepPurchasePrices()
    {
        using var host = new PurchasingTestHost();
        var id = await SeedPostedReturnAsync(host);

        var dto = (await new GetPurchaseReturnByIdQueryHandler(host.Context)
            .Handle(new GetPurchaseReturnByIdQuery(id, false), Ct)).Data;

        Assert.Null(dto.CostRemovedBase);
        Assert.All(dto.Lines, x => Assert.Null(x.CostRemovedBase));
        Assert.Equal(10m, dto.ExpectedCreditBase);
        Assert.All(dto.Lines, x => Assert.Equal(5m, x.ReceiptUnitCostBase));
    }

    [Fact]
    public async Task GetReturn_ShouldExposeCostRemoved_WithViewCost()
    {
        using var host = new PurchasingTestHost();
        var id = await SeedPostedReturnAsync(host);

        var dto = (await new GetPurchaseReturnByIdQueryHandler(host.Context)
            .Handle(new GetPurchaseReturnByIdQuery(id, true), Ct)).Data;

        Assert.Equal(14m, dto.CostRemovedBase);
        Assert.All(dto.Lines, x => Assert.Equal(14m, x.CostRemovedBase));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchReturns_ShouldMaskCostRemoved_UnlessViewCost(bool canViewCost)
    {
        using var host = new PurchasingTestHost();
        await SeedPostedReturnAsync(host);

        var result = await new SearchPurchaseReturnsQueryHandler(host.Context)
            .Handle(new SearchPurchaseReturnsQuery(new SearchPurchaseReturnRequest(), canViewCost), Ct);

        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(canViewCost, dto.CostRemovedBase.HasValue);
        Assert.All(dto.Lines, x => Assert.Equal(canViewCost, x.CostRemovedBase.HasValue));
        Assert.Equal(10m, dto.ExpectedCreditBase);
    }

    [Fact]
    public async Task GetReturn_ShouldReturnNotFound_ForAnUnknownReturn()
    {
        using var host = new PurchasingTestHost();

        var result = await new GetPurchaseReturnByIdQueryHandler(host.Context).Handle(new GetPurchaseReturnByIdQuery(999, true), Ct);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CostRemovedBase_ShouldBeNullUntilPosted()
    {
        var order = PurchasingBuilder.Approved((1, 10));
        var receipt = PurchasingBuilder.PostedReceipt(order, 5, (1, 6));
        PurchaseReturn purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2));

        Assert.Null(purchaseReturn.CostRemovedBase);
    }
}
