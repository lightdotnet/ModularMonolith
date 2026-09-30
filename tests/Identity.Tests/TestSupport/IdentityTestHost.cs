using Light.EventBus.Abstractions;
using Light.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Identity.Authorization;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.IntegrationEvents;
using StarterKit.Modules.Identity.Persistence;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared;

namespace Identity.Tests.TestSupport;

/// <summary>
/// Wires an <see cref="IdentityDbContext"/> plus a real ASP.NET Identity stack (mirroring
/// <c>Identity/DependencyInjection.cs</c>), so services under test exercise real EF Core/Identity
/// behavior (password hashing, claim/role sync, IQueryable translation) instead of hand-mocked
/// stand-ins. The store is a Sqlite in-memory database by default, or the EF Core InMemory
/// provider when <c>useInMemoryProvider</c> is set.
/// </summary>
/// <remarks>
/// The context's mediator and event bus are mocks, exposed as <see cref="Publisher"/> and
/// <see cref="EventBus"/>, so tests can verify what was published after a save. Every service
/// resolved here shares one <see cref="IntegrationEventCollector"/> with the context, as it does
/// within a request scope.
/// </remarks>
internal sealed class IdentityTestHost : IDisposable
{
    private readonly SqliteConnection? _connection;
    private readonly ServiceProvider _provider;

    public IdentityTestHost(
        FakeCurrentUser? currentUser = null,
        FakeDateTime? dateTime = null,
        bool useInMemoryProvider = false)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddSingleton(Publisher.Object);
        services.AddSingleton(EventBus.Object);
        services.AddScoped<IntegrationEventCollector>();

        if (useInMemoryProvider)
        {
            var databaseName = Guid.NewGuid().ToString();

            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(_connection));
        }

        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 3;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<IdentityDbContext>();
        Context.Database.EnsureCreated();

        IntegrationEvents = _provider.GetRequiredService<IntegrationEventCollector>();
        UserManager = _provider.GetRequiredService<UserManager<User>>();
        RoleManager = _provider.GetRequiredService<RoleManager<Role>>();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public Mock<IPublisher> Publisher { get; } = new();

    public Mock<IEventBus> EventBus { get; } = new();

    public IntegrationEventCollector IntegrationEvents { get; }

    public IdentityDbContext Context { get; }

    public UserManager<User> UserManager { get; }

    public RoleManager<Role> RoleManager { get; }

    public UserService CreateUserService() => new(UserManager, IntegrationEvents, DateTime);

    public PermissionGrantGuard CreatePermissionGrantGuard() => new(CurrentUser, UserManager, RoleManager);

    public void Dispose()
    {
        _provider.Dispose();
        _connection?.Dispose();
    }
}
