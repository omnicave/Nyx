# Repository Map

High-level orientation for tools and packages.

Root
- Nyx.sln: solution file
- Directory.Build.props: versioning + target frameworks (net8.0;net9.0)
- Directory.Packages.props: central package versions
- docs/: ADRs
- packages/: built NuGet packages (checked in)
- examples/: runnable samples
- experiments/: prototypes and spike projects
- tests/: xUnit tests

Core libraries (src/)
- CommandLine/Nyx.Cli: CLI framework built on System.CommandLine
- Data/Nyx.Data: EF Core + Postgres data access helpers
- Hosting/Nyx.Hosting: base host builder + DI auto-registration
- Hosting/Nyx.Microservice.Host: wiring for WebApi + Orleans host + minimal API
- Hosting/Nyx.Orleans.Host: Orleans silo host configuration
- Orleans/Nyx.Orleans: shared Orleans grains, indexing, jobs, Web API helpers
- Orleans/Nyx.Orleans.Client: client-side Orleans helpers + serialization
- Orleans/Nyx.Orleans.Data: data-query grains
- Orleans/Nyx.Orleans.Nats: NATS clustering/streaming integration
- Nyx.Utils: small utility types and DI helpers
- WebApi/Nyx.WebApi: MVC Web API defaults + OpenAPI/Swagger/health
- WebApi/Nyx.WebApi.Minimal: minimal API endpoint registration

Key entry points
- src/Hosting/Nyx.Hosting/BaseHostBuilder.cs
- src/Hosting/Nyx.Orleans.Host/Server/HostBuilderExtensions.cs
- src/Hosting/Nyx.Orleans.Host/OrleansHost.cs
- src/Orleans/Nyx.Orleans/Jobs/Grains/BackgroundJobGrain.cs
- src/CommandLine/Nyx.Cli/CommandLineHostBuilder.cs
- src/WebApi/Nyx.WebApi/WebApplicationBuilderExtensions.cs
