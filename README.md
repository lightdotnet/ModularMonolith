# Modular Monolith Solution Template for ASP.NET Core

## Demo
- [API](http://light-api.runasp.net/swagger/)
- [BlazorServer](http://light-demo.tryasp.net/)

## Technologies

* .NET 10
* Entity Framework Core 10
* Light Framework
* Mapster
* FluentValidation
* SignalR
* Serilog
* Redis
* RabbitMQ
* Aspire

## Getting Started
The easiest way to get started is to install the [.NET template](https://www.nuget.org/packages/ModularMonolith.Solution.Template):
```bash
dotnet new install ModularMonolith.Solution.Template
```

To create a ASP.NET Core Web API solution:
```bash
dotnet new mm-sln -n YourProjectName
```

To create module projects template:
```bash
dotnet new mm -n YourModuleName
```

To create module projects with clean architecture template:
```bash
dotnet new ca-mm -n YourModuleName
```

## Migrate data with DB Provider before run:
PostgreSQL
```bash
dotnet run -p src\Migrations\PostgreSQL\PostgreSQL.csproj
```
MSSQL
```bash
dotnet run -p src\Migrations\MSSQL\MSSQL.csproj
```
Sqlite (copy Seed.db to WebApi project after migrate)
```bash
dotnet run -p src\Migrations\Sqlite\Sqlite.csproj
```
Or use In Memory Database, you will need to update WebApi/appsettings.json as follows:
```json
  "DbProvider": "InMemory",
```

## Overview

### Building Blocks shared projects

This will contain common entities, enums, exceptions, interfaces, types, rules, etc... can dependent from modules

- Shared
- Infrastructure

### Contracts project

This projects contain share resources for other modules can reference.

- *.Contracts

### Modules project

This projects contains all owned module SOLID. It is dependent on the contracts projects.
Modules projects cannot call directly, if it need access resource from other module, it only access other module.Contracts

### Server & Client
- WebApi
- ClientApp ([MudBlazor](https://mudblazor.com/))
- Aspire support
