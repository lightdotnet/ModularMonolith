using Monolith.Catalog.Infrastructure.Mappings;
using Monolith.Database;
using Monolith.Modularity;

namespace Monolith.Catalog;

public class CatalogModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogContext>(configuration, DbConnectionNames.CATALOG);

        MapsterSettings.Configure();

        ShowModuleInfo();
    }
}
