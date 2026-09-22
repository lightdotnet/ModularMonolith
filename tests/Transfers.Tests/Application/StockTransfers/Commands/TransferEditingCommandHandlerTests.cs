using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Api.Services;
using StarterKit.Transfers.Contracts.Common;
using StarterKit.Transfers.Contracts.StockTransfers;
using Transfers.Tests.TestSupport;
using ValidationException = Light.Exceptions.ValidationException;

namespace Transfers.Tests.Application.StockTransfers.Commands;

/// <summary>Create / header / line command handlers: Draft-only editing, product and location lookups, save mapping.</summary>
public class TransferEditingCommandHandlerTests
{
    private static Mock<ICatalogPricingService> MakeCatalog(
        long productId = 5,
        string? sku = "SKU-5",
        bool exists = true)
    {
        var mock = new Mock<ICatalogPricingService>();

        mock.Setup(x => x.GetPriceInfoAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists
                ? new ProductPriceInfoDto { Id = productId, ProductName = "Widget", Sku = sku }
                : null);

        return mock;
    }

    private static Mock<ILocationDirectoryService> MakeLocations(
        LocationStatus destinationStatus = LocationStatus.Active,
        bool sourceExists = true,
        bool destinationExists = true)
    {
        var mock = new Mock<ILocationDirectoryService>();

        mock.Setup(x => x.GetAsync("src", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceExists ? new LocationDto { Id = "src", Name = "Source WH", Status = LocationStatus.Active } : null);

        mock.Setup(x => x.GetAsync("dst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationExists ? new LocationDto { Id = "dst", Name = "Dest Store", Status = destinationStatus } : null);

        return mock;
    }

    private static async Task<StockTransfer> ReloadAsync(TransfersTestHost host)
    {
        TransferSeed.Detach(host);

        return await host.Context.StockTransfers
            .Include(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    // Create

    [Fact]
    public async Task Create_ShouldPersistADraft_WithSnapshottedLocationNames()
    {
        using var host = new TransfersTestHost();
        var handler = new CreateStockTransferCommandHandler(
            host.Context,
            new TransferLocationResolver(MakeLocations().Object),
            host.DateTime,
            NullLogger<CreateStockTransferCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateStockTransferCommand(new CreateStockTransferRequest { SourceLocationId = " src ", DestinationLocationId = "dst", Note = "n" }),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host);
        Assert.Equal(result.Data, stored.Id);
        Assert.Equal(TransferStatus.Draft, stored.Status);
        Assert.Equal("Source WH", stored.SourceLocationName);
        Assert.Equal("Dest Store", stored.DestinationLocationName);
        Assert.Equal("n", stored.Note);
    }

    [Theory]
    [InlineData(false, true, LocationStatus.Active)]
    [InlineData(true, false, LocationStatus.Active)]
    [InlineData(true, true, LocationStatus.Inactive)]
    public async Task Create_ShouldFail_WhenALocationIsMissing_OrTheDestinationIsNotActive(
        bool sourceExists,
        bool destinationExists,
        LocationStatus destinationStatus)
    {
        using var host = new TransfersTestHost();
        var handler = new CreateStockTransferCommandHandler(
            host.Context,
            new TransferLocationResolver(MakeLocations(destinationStatus, sourceExists, destinationExists).Object),
            host.DateTime,
            NullLogger<CreateStockTransferCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateStockTransferCommand(new CreateStockTransferRequest { SourceLocationId = "src", DestinationLocationId = "dst" }),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.False(await host.Context.StockTransfers.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_ShouldThrowValidation_WhenSourceAndDestinationResolveToTheSameLocation()
    {
        using var host = new TransfersTestHost();
        var locations = new Mock<ILocationDirectoryService>();
        locations
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationDto { Id = "same", Name = "Same", Status = LocationStatus.Active });
        var handler = new CreateStockTransferCommandHandler(
            host.Context,
            new TransferLocationResolver(locations.Object),
            host.DateTime,
            NullLogger<CreateStockTransferCommandHandler>.Instance);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateStockTransferCommand(new CreateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "b" }),
            TestContext.Current.CancellationToken));
    }

    // Header

    [Fact]
    public async Task UpdateHeader_ShouldApplyTheResolvedLocations_ToADraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = new UpdateStockTransferCommandHandler(host.Context, new TransferLocationResolver(MakeLocations().Object));

        var result = await handler.Handle(
            new UpdateStockTransferCommand(transfer.Id, new UpdateStockTransferRequest { SourceLocationId = "src", DestinationLocationId = "dst", Note = "x" }),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host);
        Assert.Equal("src", stored.SourceLocationId);
        Assert.Equal("Dest Store", stored.DestinationLocationName);
    }

    [Fact]
    public async Task UpdateHeader_ShouldReturnNotFound_ForAnUnknownTransfer_AndFailForABadLocation()
    {
        using var host = new TransfersTestHost();
        var missing = new UpdateStockTransferCommandHandler(host.Context, new TransferLocationResolver(MakeLocations().Object));
        var request = new UpdateStockTransferRequest { SourceLocationId = "src", DestinationLocationId = "dst" };

        Assert.False((await missing.Handle(new UpdateStockTransferCommand(999, request), TestContext.Current.CancellationToken)).IsSuccess);

        var transfer = await TransferSeed.DraftAsync(host);
        var badLocation = new UpdateStockTransferCommandHandler(host.Context, new TransferLocationResolver(MakeLocations(destinationExists: false).Object));

        Assert.False((await badLocation.Handle(new UpdateStockTransferCommand(transfer.Id, request), TestContext.Current.CancellationToken)).IsSuccess);
    }

    [Fact]
    public async Task UpdateHeader_ShouldThrowConflict_ForADispatchedTransfer()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var handler = new UpdateStockTransferCommandHandler(host.Context, new TransferLocationResolver(MakeLocations().Object));

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateStockTransferCommand(transfer.Id, new UpdateStockTransferRequest { SourceLocationId = "src", DestinationLocationId = "dst" }),
            TestContext.Current.CancellationToken));
    }

    // Add line

    [Fact]
    public async Task AddLine_ShouldSnapshotProductNameAndSku_AndPersist()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10));
        var handler = new AddStockTransferLineCommandHandler(host.Context, MakeCatalog().Object);

        var result = await handler.Handle(
            new AddStockTransferLineCommand(transfer.Id, new AddStockTransferLineRequest { ProductId = 5, Quantity = 3 }),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var line = (await ReloadAsync(host)).Lines.Single(x => x.ProductId == 5);
        Assert.Equal("Widget", line.ProductName);
        Assert.Equal("SKU-5", line.Sku);
        Assert.Equal(3, line.RequestedQuantity);
    }

    [Fact]
    public async Task AddLine_ShouldStoreAnEmptySku_WhenTheProductHasNone()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10));
        var handler = new AddStockTransferLineCommandHandler(host.Context, MakeCatalog(sku: null).Object);

        await handler.Handle(
            new AddStockTransferLineCommand(transfer.Id, new AddStockTransferLineRequest { ProductId = 5, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, (await ReloadAsync(host)).Lines.Single(x => x.ProductId == 5).Sku);
    }

    [Fact]
    public async Task AddLine_ShouldReturnNotFound_ForAnUnknownTransferOrProduct()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var request = new AddStockTransferLineRequest { ProductId = 5, Quantity = 1 };

        var noTransfer = await new AddStockTransferLineCommandHandler(host.Context, MakeCatalog().Object)
            .Handle(new AddStockTransferLineCommand(999, request), TestContext.Current.CancellationToken);
        var noProduct = await new AddStockTransferLineCommandHandler(host.Context, MakeCatalog(exists: false).Object)
            .Handle(new AddStockTransferLineCommand(transfer.Id, request), TestContext.Current.CancellationToken);

        Assert.False(noTransfer.IsSuccess);
        Assert.False(noProduct.IsSuccess);
    }

    [Fact]
    public async Task AddLine_ShouldReject_ADuplicateProduct_AndANonDraftTransfer()
    {
        using var host = new TransfersTestHost();
        var draft = await TransferSeed.DraftAsync(host, (5, 10));
        var handler = new AddStockTransferLineCommandHandler(host.Context, MakeCatalog().Object);
        var request = new AddStockTransferLineRequest { ProductId = 5, Quantity = 1 };

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new AddStockTransferLineCommand(draft.Id, request), TestContext.Current.CancellationToken));

        draft.Cancel("x", host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new AddStockTransferLineCommand(draft.Id, request), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddLine_ShouldMapAConcurrencyLoss_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = new AddStockTransferLineCommandHandler(host.Context, MakeCatalog().Object);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new AddStockTransferLineCommand(transfer.Id, new AddStockTransferLineRequest { ProductId = 5, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    // Update line

    [Fact]
    public async Task UpdateLine_ShouldChangeTheQuantity_AndMapErrors()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10));
        var lineId = transfer.Lines[0].Id;
        var handler = new UpdateStockTransferLineCommandHandler(host.Context);

        var ok = await handler.Handle(
            new UpdateStockTransferLineCommand(transfer.Id, lineId, new UpdateStockTransferLineRequest { Quantity = 7 }),
            TestContext.Current.CancellationToken);
        var missing = await handler.Handle(
            new UpdateStockTransferLineCommand(999, lineId, new UpdateStockTransferLineRequest { Quantity = 7 }),
            TestContext.Current.CancellationToken);

        Assert.True(ok.IsSuccess);
        Assert.False(missing.IsSuccess);
        Assert.Equal(7, (await ReloadAsync(host)).Lines[0].RequestedQuantity);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateStockTransferLineCommand(transfer.Id, 12345, new UpdateStockTransferLineRequest { Quantity = 1 }),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateLine_ShouldMapAConcurrencyLoss_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10));
        var handler = new UpdateStockTransferLineCommandHandler(host.Context);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateStockTransferLineCommand(transfer.Id, transfer.Lines[0].Id, new UpdateStockTransferLineRequest { Quantity = 2 }),
            TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    [Fact]
    public async Task UpdateLine_ShouldThrowConflict_WhenTheTransferIsNoLongerADraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var handler = new UpdateStockTransferLineCommandHandler(host.Context);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateStockTransferLineCommand(transfer.Id, transfer.Lines[0].Id, new UpdateStockTransferLineRequest { Quantity = 2 }),
            TestContext.Current.CancellationToken));
    }

    // Remove line

    [Fact]
    public async Task RemoveLine_ShouldRemoveTheLine_AndMapErrors()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10), (2, 4));
        var handler = new RemoveStockTransferLineCommandHandler(host.Context);
        var lineId = transfer.Lines[0].Id;

        var ok = await handler.Handle(new RemoveStockTransferLineCommand(transfer.Id, lineId), TestContext.Current.CancellationToken);
        var missing = await handler.Handle(new RemoveStockTransferLineCommand(999, lineId), TestContext.Current.CancellationToken);

        Assert.True(ok.IsSuccess);
        Assert.False(missing.IsSuccess);
        Assert.Single((await ReloadAsync(host)).Lines);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new RemoveStockTransferLineCommand(transfer.Id, lineId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveLine_ShouldMapAConcurrencyLoss_ToConflict_AndRejectANonDraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10), (2, 4));
        var handler = new RemoveStockTransferLineCommandHandler(host.Context);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RemoveStockTransferLineCommand(transfer.Id, transfer.Lines[0].Id), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);

        using var second = new TransfersTestHost();
        var dispatched = await TransferSeed.DispatchedAsync(second, (1, 10));
        var secondHandler = new RemoveStockTransferLineCommandHandler(second.Context);

        await Assert.ThrowsAsync<ConflictException>(() => secondHandler.Handle(
            new RemoveStockTransferLineCommand(dispatched.Id, dispatched.Lines[0].Id),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CommandValidators_ShouldRequirePositiveIds()
    {
        Assert.False(new UpdateStockTransferCommandValidator()
            .Validate(new UpdateStockTransferCommand(0, new UpdateStockTransferRequest { SourceLocationId = "a", DestinationLocationId = "b" })).IsValid);
        Assert.False(new AddStockTransferLineCommandValidator()
            .Validate(new AddStockTransferLineCommand(0, new AddStockTransferLineRequest { ProductId = 1, Quantity = 1 })).IsValid);
        Assert.False(new UpdateStockTransferLineCommandValidator()
            .Validate(new UpdateStockTransferLineCommand(1, 0, new UpdateStockTransferLineRequest { Quantity = 1 })).IsValid);
        Assert.False(new RemoveStockTransferLineCommandValidator()
            .Validate(new RemoveStockTransferLineCommand(1, 0)).IsValid);
        Assert.True(new RemoveStockTransferLineCommandValidator()
            .Validate(new RemoveStockTransferLineCommand(1, 1)).IsValid);
    }
}
