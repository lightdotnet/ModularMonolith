var builder = DistributedApplication.CreateBuilder(args);

// Running the API directly (outside Aspire) keeps appsettings.json defaults:
// in-memory cache and the no-op event bus. Host ports below are pinned because the API
// reads fixed host/port settings (Caching:RedisHost, RabbitMQ:Host) rather than Aspire
// connection strings.
var api = builder.AddProject<Projects.StarterKit_WebApi>("api");

// Redis: distributed cache (Caching section).
var redis = builder.AddRedis("redis", port: 6379)
    .WithDataVolume();

api
    .WithEnvironment("Caching__Provider", "redis")
    .WithEnvironment("Caching__RedisHost", "localhost:6379")
    .WithEnvironment("Caching__RedisPassword", redis.Resource.PasswordParameter!)
    .WaitFor(redis);

// RabbitMQ: integration-event bus (RabbitMQ section).
var rabbitmq = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithManagementPlugin()
    .WithDataVolume();

api
    .WithEnvironment("RabbitMQ__Enable", "true")
    .WithEnvironment("RabbitMQ__Host", "localhost")
    .WithEnvironment("RabbitMQ__Username", rabbitmq.Resource.UserNameReference)
    .WithEnvironment("RabbitMQ__Password", rabbitmq.Resource.PasswordParameter)
    .WaitFor(rabbitmq);

builder.Build().Run();
