var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.StarterKit_WebApi>("api");

builder.Build().Run();
