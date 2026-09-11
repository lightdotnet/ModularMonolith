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