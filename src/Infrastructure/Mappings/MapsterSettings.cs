using Mapster;
using StarterKit.Shared;

namespace StarterKit.Infrastructure.Mappings;

public class MapsterSettings
{
    public static void Configure()
    {
        // here we will define the type conversion / Custom-mapping
        // More details at https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping

        // Mapster maps Status -> Status.ActiveStatus by convention already (both expose a "Value" property),
        // but this makes the mapping explicit so it keeps working if Status's shape changes later.
        StatusConfigure();
    }

    private static void StatusConfigure()
    {
        TypeAdapterConfig<ActiveStatus, ActiveStatus.State>
            .NewConfig()
            .Map(dest => dest, src => src.Value);
    }
}