using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Contracts.Services;

namespace StarterKit.Locations.Api.Services;

internal class LocationDirectoryService(LocationDbContext context) : ILocationDirectoryService
{
    public Task<LocationDto?> GetAsync(
        string locationId,
        CancellationToken cancellationToken = default) =>
        context.Locations
            .AsNoTracking()
            .Where(x => x.Id == locationId)
            .Select(x => new LocationDto
            {
                Id = x.Id,
                ParentLocationId = x.ParentLocationId,
                LocationTypeId = x.LocationTypeId,
                Name = x.Name,
                Code = x.Code,
                Status = x.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        string locationId,
        CancellationToken cancellationToken = default) =>
        context.Locations
            .AsNoTracking()
            .AnyAsync(x => x.Id == locationId, cancellationToken);

    public async Task<IReadOnlyList<LocationLookupDto>> GetChildrenAsync(
        string locationId,
        CancellationToken cancellationToken = default) =>
        await context.Locations
            .AsNoTracking()
            .Where(x => x.ParentLocationId == locationId)
            .Select(x => new LocationLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                LocationTypeId = x.LocationTypeId,
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationLookupDto>> GetLookupAsync(
        CancellationToken cancellationToken = default) =>
        await context.Locations
            .AsNoTracking()
            .Select(x => new LocationLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                LocationTypeId = x.LocationTypeId,
            })
            .ToListAsync(cancellationToken);
}
