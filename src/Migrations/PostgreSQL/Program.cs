using Microsoft.Extensions.DependencyInjection;
using PostgreSQL;

// set Environment
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Live");

using var host = Host.CreateHostBuilder(args).Build();

using var scope = host.Services.CreateScope();

var serviceProvider = scope.ServiceProvider;

var identityInitialiser = serviceProvider.GetRequiredService<IdentityContextInitialiser>();

await identityInitialiser.InitialiseAsync();

await identityInitialiser.TrySeedAsync();

var organizationInitialiser = serviceProvider.GetRequiredService<OrganizationContextInitialiser>();

await organizationInitialiser.InitialiseAsync();

await organizationInitialiser.TrySeedAsync();

var approvalInitialiser = serviceProvider.GetRequiredService<StarterKit.Approval.Api.Data.ApprovalContextInitialiser>();

await approvalInitialiser.InitialiseAsync();

var leaveManagementInitialiser = serviceProvider.GetRequiredService<StarterKit.LeaveManagement.Api.Data.LeaveManagementContextInitialiser>();

await leaveManagementInitialiser.InitialiseAsync();

// Location module
var locationInitialiser = serviceProvider.GetRequiredService<StarterKit.Locations.Api.Data.LocationContextInitialiser>();

await locationInitialiser.InitialiseAsync();

await locationInitialiser.TrySeedAsync();

// Catalog module
var catalogInitialiser = serviceProvider.GetRequiredService<StarterKit.Catalog.Api.Data.CatalogContextInitialiser>();

await catalogInitialiser.InitialiseAsync();

await catalogInitialiser.TrySeedAsync();

// Orders module
var ordersInitialiser = serviceProvider.GetRequiredService<StarterKit.Orders.Api.Data.OrdersContextInitialiser>();

await ordersInitialiser.InitialiseAsync();

await ordersInitialiser.TrySeedAsync();

// Inventory module
var inventoryInitialiser = serviceProvider.GetRequiredService<StarterKit.Inventory.Api.Data.InventoryContextInitialiser>();

await inventoryInitialiser.InitialiseAsync();

// Transfers module
var transfersInitialiser = serviceProvider.GetRequiredService<StarterKit.Transfers.Api.Data.TransfersContextInitialiser>();

await transfersInitialiser.InitialiseAsync();

// Purchasing module
var purchasingInitialiser = serviceProvider.GetRequiredService<StarterKit.Purchasing.Api.Data.PurchasingContextInitialiser>();

await purchasingInitialiser.InitialiseAsync();

// Currency module
var currencyInitialiser = serviceProvider.GetRequiredService<StarterKit.Currencies.Api.Data.CurrencyContextInitialiser>();

await currencyInitialiser.InitialiseAsync();

await currencyInitialiser.TrySeedAsync();
