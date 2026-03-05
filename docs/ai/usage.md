# Usage Patterns

Build and test
- Build solution: `dotnet build Nyx.sln`
- Run tests: `dotnet test Nyx.sln`

Typical usage: Orleans host
- Configure `nyx:orleans` in appsettings or environment variables.
- Use `HostBuilderExtensions.ConfigureSiloHost(...)` to configure the silo host.
- Add grain storage/clustering configs; optional schema bootstrap runs automatically.

Typical usage: Web API
- Call `ConfigureWebApiWithDefaults` on WebApplicationBuilder.
- Call `ConfigureWebApiDefaults` on the app.

Typical usage: Minimal API
- Implement `IEndpointConfiguration` in your assembly.
- Call `AutoRegisterEndpointsFromAssembly` and `MapEndpoints`.

Typical usage: CLI
- Add `[CliCommand("name")]` to command classes.
- Build via `CommandLineHostBuilder.Create(args)`.
- Register commands from assembly or explicitly.

Key configuration files
- `Directory.Build.props`: target frameworks and version prefix
- `Directory.Packages.props`: central package versions
