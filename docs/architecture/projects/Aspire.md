# Project Overview: Aspire (StarterKit.AppHost, StarterKit.ServiceDefaults)

The .NET Aspire integration is two projects in the `/src/` solution folder:

| Project | Assembly | Role |
|---|---|---|
| `src/StarterKit.AppHost` | `StarterKit.AppHost` | Aspire app host: the local-development orchestrator that launches the API, its Redis and RabbitMQ containers, the admin client's dev server, and the Aspire dashboard |
| `src/StarterKit.ServiceDefaults` | `StarterKit.ServiceDefaults` | Aspire shared project: the service defaults (telemetry, service discovery, HttpClient resilience, health endpoints) that the API applies at startup |

## Purpose

- **`StarterKit.AppHost`** runs the solution for local development. It adds [StarterKit.WebApi](WebApi.md) as the resource `api`, a Redis container for the distributed cache, a RabbitMQ container for the integration-event bus, and the admin client (`clients/admin`) dev server as the resource `next-admin`, and starts the Aspire dashboard, which shows the resources, their endpoints, and the logs, metrics, and traces the API exports. It is not deployed; `StarterKit.WebApi` stays the only deployable.
- **`StarterKit.ServiceDefaults`** is the Aspire service-defaults template, unmodified. It is the one place the API's OpenTelemetry, service-discovery, and HttpClient-resilience setup comes from.

## Public Surface

**`StarterKit.ServiceDefaults`** — one static class, `Extensions`, in the namespace `Microsoft.Extensions.Hosting` (the template's choice, so the methods are in scope wherever the hosting namespace is):

| Member | Role |
|---|---|
| `AddServiceDefaults<TBuilder>()` | Calls `ConfigureOpenTelemetry` and `AddDefaultHealthChecks`, registers service discovery, and configures every `HttpClient` with the standard resilience handler and service discovery |
| `ConfigureOpenTelemetry<TBuilder>()` | Adds the OpenTelemetry logging provider (formatted messages and scopes included); metrics from ASP.NET Core, HttpClient, and the runtime; traces from the application-name source, ASP.NET Core (requests to `/health` and `/alive` excluded), and HttpClient. The OTLP exporter is added only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set |
| `AddDefaultHealthChecks<TBuilder>()` | Registers the `self` health check (always healthy), tagged `live` |
| `MapDefaultEndpoints(WebApplication)` | In `Development` only: maps `/health` (every registered check) and `/alive` (checks tagged `live`); maps nothing in other environments |

**`StarterKit.AppHost`** — an application with no public surface. `AppHost.cs` builds a `DistributedApplication` with four resources:

| Resource | Kind | Wiring |
|---|---|---|
| `api` | project (`StarterKit.WebApi`) | Receives the cache and bus settings below as environment variables and waits for `redis` and `rabbitmq` |
| `redis` | Redis container, host port 6379, data volume | `api` gets `Caching__Provider=redis`, `Caching__RedisHost`, and `Caching__RedisPassword` (the resource's generated password) |
| `rabbitmq` | RabbitMQ container, host port 5672, management plugin, data volume | `api` gets `RabbitMQ__Enable=true`, `RabbitMQ__Host`, `RabbitMQ__Username`, and `RabbitMQ__Password` (the resource's credentials) |
| `next-admin` | executable: `pnpm dev` in `clients/admin`, HTTP endpoint on port 3000 | References `api` (`WithReference`) |

The host ports are pinned because the API reads fixed host settings (`Caching:RedisHost`, `RabbitMQ:Host`), not Aspire connection strings.

## How StarterKit.WebApi Uses the Service Defaults

- **Startup**: `Program` calls `builder.AddServiceDefaults()` before registering its own services and `app.MapDefaultEndpoints()` after endpoint mapping — see [WebApi § Public Surface](WebApi.md#public-surface).
- **Logging**: Serilog stays the logging pipeline. `Program` clears the default logging providers, `AddServiceDefaults` adds the OpenTelemetry provider, and Serilog is configured with the vendor `SerilogConfigurationExtensions.Configure` and `writeToProviders: true`, so Serilog events go both to Serilog's own sinks and to the OpenTelemetry provider, which exports them to the dashboard.
- **Health endpoints**:

  | Endpoint | Mapped by | Environments | Use |
  |---|---|---|---|
  | `/hc` | [Infrastructure](Infrastructure.md) (`MapHealthChecksEndpoint`) | All | The deployment health endpoint |
  | `/health`, `/alive` | `MapDefaultEndpoints` | `Development` only | Aspire readiness and liveness |

  All three report the same registrations; in `StarterKit.WebApi` the only registered check is `self`.

## Running

```
dotnet run --project src/StarterKit.AppHost
```

A container runtime is required for the Redis and RabbitMQ resources, and `pnpm` for the `next-admin` resource. The AppHost's launch profiles (`src/StarterKit.AppHost/Properties/launchSettings.json`), `https` and `http`, run it in `Development`, set the dashboard's own URL and its OTLP and resource-service endpoints, and open the dashboard in the browser. The AppHost passes the `OTEL_*` settings, including `OTEL_EXPORTER_OTLP_ENDPOINT`, to the `api` resource, together with the cache and bus settings above. `aspire.config.json` points the Aspire CLI at the AppHost project.

Under the AppHost the API therefore uses the Redis cache and the RabbitMQ bus. Run directly, it keeps its `appsettings.json` defaults — the in-memory cache and the no-op bus — and exports no telemetry; its other configuration is the same either way — see [development-guide.md § Running Locally](../../conventions/development-guide.md#running-locally).

## Dependencies

| Project | Depends on | Type (project/package/SDK) | Why |
|---|---|---|---|
| `StarterKit.AppHost` | `StarterKit.WebApi` | project | The `api` resource (`Projects.StarterKit_WebApi`) |
| `StarterKit.AppHost` | `Aspire.AppHost.Sdk` | project SDK | Aspire hosting and the dashboard |
| `StarterKit.AppHost` | `Aspire.Hosting.Redis`, `Aspire.Hosting.RabbitMQ` | package | The `redis` and `rabbitmq` resources |
| `StarterKit.ServiceDefaults` | `Microsoft.AspNetCore.App` | framework reference | Health-check endpoint mapping |
| `StarterKit.ServiceDefaults` | `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery` | package | HttpClient resilience and service discovery |
| `StarterKit.ServiceDefaults` | `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` | package | Telemetry and the OTLP exporter |

`StarterKit.ServiceDefaults` references no solution project. Both projects version their Aspire and OpenTelemetry dependencies themselves (`ManagePackageVersionsCentrally=false`) rather than through `Directory.Packages.props` — see [dependency-graph.md § Version Mismatches](../dependency-graph.md#version-mismatches). Full reference graph: [dependency-graph.md](../dependency-graph.md).

## Depended On By

| Project | Referenced by |
|---|---|
| `StarterKit.AppHost` | Nothing |
| `StarterKit.ServiceDefaults` | `StarterKit.WebApi` only |

No framework project, module, migrator, or test project references either one.

## Notable Conventions

- The folder names are the full project names (`src/StarterKit.AppHost`, `src/StarterKit.ServiceDefaults`), as for `src/StarterKit.WebApi`.
- `StarterKit.ServiceDefaults` is kept as the Aspire template ships it; API-specific composition belongs in [StarterKit.WebApi](WebApi.md), not here.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-10-01_
