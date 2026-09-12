using Microsoft.Extensions.DependencyInjection;
using MSSQL;
using StarterKit.Notifications.Api.Data;

// set Environment
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Live");

using var host = Host.CreateHostBuilder(args).Build();

using var scope = host.Services.CreateScope();

var serviceProvider = scope.ServiceProvider;
/*
// Identity module
var identityInitialiser = serviceProvider.GetRequiredService<IdentityContextInitialiser>();

await identityInitialiser.InitialiseAsync();

await identityInitialiser.TrySeedAsync();

// Notification module
var notificationInitialiser = serviceProvider.GetRequiredService<NotificationContextInitialiser>();

await notificationInitialiser.InitialiseAsync();

// Organization module
var organizationInitialiser = serviceProvider.GetRequiredService<OrganizationContextInitialiser>();

await organizationInitialiser.InitialiseAsync();

await organizationInitialiser.TrySeedAsync();

// Approval module
var approvalInitialiser = serviceProvider.GetRequiredService<StarterKit.Approval.Api.Data.ApprovalContextInitialiser>();

await approvalInitialiser.InitialiseAsync();

// LeaveManagement module
var leaveManagementInitialiser = serviceProvider.GetRequiredService<StarterKit.LeaveManagement.Api.Data.LeaveManagementContextInitialiser>();

await leaveManagementInitialiser.InitialiseAsync();
*/
// Location module
var locationInitialiser = serviceProvider.GetRequiredService<StarterKit.Locations.Api.Data.LocationContextInitialiser>();

await locationInitialiser.InitialiseAsync();

await locationInitialiser.TrySeedAsync();

// Catalog module
var catalogInitialiser = serviceProvider.GetRequiredService<StarterKit.Catalog.Api.Data.CatalogContextInitialiser>();

await catalogInitialiser.InitialiseAsync();