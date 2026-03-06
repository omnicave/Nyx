# Usage Patterns

## Setting up Orleans hosts
- Configure `nyx:orleans` in appsettings or environment variables.
- Use `HostBuilderExtensions.ConfigureSiloHost(...)` to configure the silo host.
- Add grain storage/clustering configs; optional schema bootstrap runs automatically.

## Setting up Web API
- Call `ConfigureWebApiWithDefaults` on WebApplicationBuilder.
- Call `ConfigureWebApiDefaults` on the app.

## Setting up Minimal Web API
- Implement `IEndpointConfiguration` in your assembly.
- Call `AutoRegisterEndpointsFromAssembly` and `MapEndpoints`.

## CLI
- Add `[CliCommand("name")]` to command classes.
- Build via `CommandLineHostBuilder.Create(args)`.
- Register commands from assembly or explicitly.

## Dependency wiring

Pattern: direct source references or NuGet packages
- Source references are common for local development:
  - Use `$(NyxSourcesPath)` or relative paths to `Nyx.Framework/src/...` in `.csproj` files.
- Some tools use NuGet packages for host/clients.

Typical references
- `Nyx.Orleans.Host` for Orleans hosting
- `Nyx.Orleans.Client` for Orleans client usage
- `Nyx.Orleans` for contracts, indexing, management, and web API helpers
- `Nyx.Orleans.Data` + `Nyx.Data` for data access
- `Nyx.Hosting` + `Nyx.Hosting.Modules` for modular setup
- `Nyx.Hosting.DependencyInjection` for DI auto-registration
- `Nyx.Utils` for value collection helpers
- `Nyx.Cli` for CLI-based hosts

## Hosting abstractions

Pattern A: Orleans-centric host builder
- `OrleansSiloHostBuilder.CreateSiloHost(...)`
- `.ConfigureClustering(...)` for clustering configuration
- `.ConfigureOrleansSilo(...)` for silo-level registration and serialization
- `.ConfigureWebApplication(...)` to attach Web API endpoints

Pattern B: Microservice host wrapper
- `WebApplication.CreateBuilder(args)`
- `builder.Configure[ServiceName]Service(...)` (extension built on Nyx.Microservice.Host)
- `.ConfigureModule<TModule>()` to compose feature modules
- `app.UseNyxMicroservice()` to apply standard middleware and endpoints

## Module system

Pattern: `INyxModule`
- Modules implement `INyxModule` to register services and hosted services.
- Host configuration composes modules:
  - `.ConfigureModule<SomeModule>()`

## DI auto-registration

Pattern: attribute-based registration
- `[TransientService]`, `[ScopedService]`, `[SingletonService]` on services/handlers.
- Commonly applied to message handlers and background services to avoid manual registration.

## Data access

Pattern: context factory + repository
- `IDataOperationContextFactory.GetSimpleOperationContext()` / `GetTransactionalOperationContext()`
- Use `IEntityRepository` from the context for queries and upserts.
- DbContext is configured via `RegisterDbContext(...)` and migrations via `RegisterDataMigrationStartupService()`.

## Orleans data + query patterns

Pattern: query grains with paging/filtering
- `ClusterClient.GetQueryGrain<T>()`
- `QueryParameters` + `QueryFilter` + `PagedResult`
- Used in Web API controllers to implement query endpoints.

## Orleans management

Pattern: management grains
- `ClusterClient.GetManagementGrain<T>()`
- Used for lookup and management operations in controllers and services.

## Web API helpers

Pattern: base controller for Orleans API
- Controllers derive from `Nyx.Orleans.WebApi.BaseClusterClientController`.
- Uses `Result` / `PagedResult` helpers for consistent API responses.

## NATS + streaming

Pattern: NATS streams for Orleans and message bus
- `Nyx.Orleans.Nats` for NATS clustering/streams.
- `AddNatsStreams(...)` with `NatsSiloPersistentStreamConfigurator`.
- Message bus uses `INatsSerializerStore` from `Nyx.Orleans.Serialization`.

## Serialization defaults

Pattern: Orleans serializer defaults + Newtonsoft filters
- `AddOrleansSerializationDefaults()`
- Custom type filters for domain namespaces.

## CLI hosting

Pattern: CLI host
- `Nyx.Cli.CommandLineHostBuilder.Create(...)`
- `RegisterCommandsFromThisAssembly()` for command discovery

## Microservice concept usage

Pattern: separate service per domain
- Each service configures its own modules, data access, and Web API surface.
- Hosts are composed as microservices with module registration, data configuration, and Orleans grains.

Key configuration files
- `Directory.Build.props`: target frameworks and version prefix
- `Directory.Packages.props`: central package versions
