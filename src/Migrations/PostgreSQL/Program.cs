using Microsoft.Extensions.DependencyInjection;
using PostgreSQL;

// set Environment
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Live");

using var host = Host.CreateHostBuilder(args).Build();

using var scope = host.Services.CreateScope();

var serviceProvider = scope.ServiceProvider;

// Identity module
var identityInitialiser = serviceProvider.GetRequiredService<IdentityContextInitialiser>();

await identityInitialiser.InitialiseAsync();

await identityInitialiser.TrySeedAsync();
