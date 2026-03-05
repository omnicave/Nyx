# Architecture Notes

This repository is a modular toolkit for Orleans-based services, with optional Web API and CLI layers.

Core patterns
- Host builder pattern: BaseHostBuilder collects host/app operations and applies them to a real host builder.
- Attribute-driven DI: [SingletonService], [ScopedService], [TransientService] auto-register via assembly scan.
- Orleans-first: shared grains + client helpers + host configuration + NATS support.

Orleans hosting
- Configuration is read from configuration section "nyx:orleans".
- Clustering supports LocalHost, Development, AdoNet (PostgreSQL), and Redis.
- Grain storage supports Memory, AdoNet (PostgreSQL), and Redis.
- Optional schema bootstrap via hosted service in Nyx.Orleans.Host.

Web API defaults
- Adds MVC controllers + Newtonsoft.Json + System.Text.Json defaults.
- Adds OpenAPI/Swagger + Scalar UI.
- Adds health checks on a separate port.

CLI framework
- Commands are discovered via [CliCommand] attribute.
- Root command is built and commands are wired to a host builder factory.
- Designed to integrate DI and configuration with System.CommandLine.

Serialization
- Newtonsoft defaults for Orleans serialization are centralized in Nyx.Orleans.Client.
- Orleans host uses these settings for AdoNet/Redis grain storage.
