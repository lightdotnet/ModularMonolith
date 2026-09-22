using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using StarterKit.Locations.Contracts.Services;

namespace StarterKit.Transfers.Api.Services;

/// <summary>Outcome of resolving a transfer's two locations; either both are set or <see cref="Error"/> is.</summary>
internal sealed record TransferLocations(
    LocationDto? Source,
    LocationDto? Destination,
    string? Error,
    bool IsNotFound)
{
    public bool Failed => Error is not null;

    public IResult ToFailure() =>
        IsNotFound
            ? Result.NotFound(Error!)
            : Result.Error(Error!);

    public IResult<T> ToFailure<T>() =>
        IsNotFound
            ? Result<T>.NotFound(Error!)
            : Result<T>.Error(Error!);
}

/// <summary>
/// Looks both locations up through Location's cross-module seam, so handlers snapshot canonical ids
/// and names: both must exist, and the destination must be active (stock cannot be sent somewhere that
/// is closed).
/// </summary>
internal class TransferLocationResolver(ILocationDirectoryService locationDirectoryService)
{
    public async Task<TransferLocations> ResolveAsync(
        string sourceLocationId,
        string destinationLocationId,
        CancellationToken cancellationToken)
    {
        var source = await locationDirectoryService.GetAsync(sourceLocationId.Trim(), cancellationToken);

        if (source is null)
            return new TransferLocations(null, null, $"Location {sourceLocationId} not found", true);

        var destination = await locationDirectoryService.GetAsync(destinationLocationId.Trim(), cancellationToken);

        if (destination is null)
            return new TransferLocations(null, null, $"Location {destinationLocationId} not found", true);

        if (destination.Status != LocationStatus.Active)
            return new TransferLocations(null, null, $"Destination location {destination.Name} is not active", false);

        return new TransferLocations(source, destination, null, false);
    }
}
