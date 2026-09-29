# StarterKit — Modular Monolith Framework Core for ASP.NET Core

The framework layer of the StarterKit Modular Monolith template: the reusable C#/.NET building blocks (built on the private "Light" framework family — `Lightsoft.*` packages) that business modules and host applications are built on, with their tests. Business modules, hosts and client apps are not part of this solution.

## Structure

```text
StarterKit.slnx
├── src/Shared                          (shared kernel — leaf, no solution dependencies)
├── src/Infrastructure                  → Shared
├── src/Persistence                     → Shared
├── src/EventBusMassTransitRabbitMQ     → Shared (integration-event bus)
└── tests/Framework.Tests               → Shared, Infrastructure, Persistence
```

Project responsibilities and the dependency rules are described in [CLAUDE.md](CLAUDE.md#1-repository-purpose).

## Tech Stack

| Layer | Stack |
|---|---|
| Runtime | ASP.NET Core (C#), `net10.0` |
| Data access | EF Core — provider-configurable via `DbProvider` (`InMemory` / `PostgreSQL` / `MSSQL` / `Sqlite`) |
| Messaging | MassTransit over RabbitMQ for integration events; a no-op bus when disabled |
| Vendor framework | `Lightsoft.*` package family (mediator, `Result`/`Paged` contracts, domain base types, ASP.NET Core authorization/modularity helpers, caching, Serilog, event bus) |
| Validation / mapping | FluentValidation, Mapster |
| Testing | xUnit v3 on Microsoft.Testing.Platform (`tests/Framework.Tests`), hand-written fakes and real Sqlite in-memory DbContexts |

Package versions are managed centrally in [Directory.Packages.props](Directory.Packages.props).

## Getting Started

```bash
dotnet build StarterKit.slnx
dotnet test tests/Framework.Tests/Framework.Tests.csproj
```

On the .NET 10 SDK, if `dotnet test` refuses the legacy VSTest path ("opt-in to the new dotnet test experience"), run the built test executable directly: `tests/Framework.Tests/bin/Debug/net10.0/Framework.Tests.exe` (filters: `-class <FQN>` / `-method <FQN>`).

Building and testing need no database server or message broker. A RabbitMQ broker is only needed at runtime by a host that enables the event bus — see [EventBusMassTransitRabbitMQ § Configuration](src/docs/architecture/EventBusMassTransitRabbitMQ.md#configuration).

## Documentation

- [CLAUDE.md](CLAUDE.md) — repository-wide entry point (project map, dependency direction, framework conventions, AI operating rules).
- [src/docs/](src/docs/) — generated project documentation.
- [.claude/](.claude/) — reusable Claude development infrastructure (agents, skills, workflows), not project documentation.

## License

MIT — see [LICENSE](LICENSE).
