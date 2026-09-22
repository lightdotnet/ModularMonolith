using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using StarterKit.Locations.Contracts.Services;

namespace StarterKit.Purchasing.Api.Services;

/// <summary>Outcome of resolving a purchase order's receiving location; either <see cref="Location"/> or <see cref="Error"/> is set.</summary>
internal sealed record ReceivingLocation(
    LocationDto? Location,
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
/// Looks the receiving location up through Location's cross-module seam, so handlers snapshot its
/// canonical id and name: it must exist and be active (goods cannot be delivered somewhere closed).
/// </summary>
internal class ReceivingLocationResolver(ILocationDirectoryService locationDirectoryService)
{
    public async Task<ReceivingLocation> ResolveAsync(
        string locationId,
        CancellationToken cancellationToken)
    {
        var location = await locationDirectoryService.GetAsync(locationId.Trim(), cancellationToken);

        if (location is null)
            return new ReceivingLocation(null, $"Location {locationId} not found", true);

        if (location.Status != LocationStatus.Active)
            return new ReceivingLocation(null, $"Receiving location {location.Name} is not active", false);

        return new ReceivingLocation(location, null, false);
    }
}
