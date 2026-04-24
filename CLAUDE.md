# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.Runtime.IdentityProvider**, the Runtime-layer library for the Cirreum Identity provider family. Single responsibility: bootstrap any `IdentityProviderRegistrar<TSettings, TInstanceSettings>` from `Cirreum:Identity:Providers:{ProviderName}` configuration, handle both phases of the registrar lifecycle (services + deferred endpoints), and stash the endpoints-phase closure as an `IdentityProviderMapping` in DI.

## Architecture

### Responsibilities

- **Services phase (immediate):** bind config → run `registrar.Register(...)` against `IServiceCollection`
- **Endpoints phase (deferred):** wrap `registrar.Map(...)` as a closure, register as `IdentityProviderMapping` singleton in DI — Runtime Extensions layer invokes it at `Map*Identity()` time
- **Dedup:** marker-type registration prevents duplicate registration of the same `TRegistrar`
- **Config missing:** skip with debug log, unless `required: true`

### Parallel to `Cirreum.Runtime.AuthorizationProvider`

Same SRP-split pattern, but:
- AuthZ helper receives `AuthenticationBuilder` (available at builder time) and calls `registrar.Register(...)` with it — single-phase.
- Identity helper has no equivalent builder-time argument for endpoints — HTTP routes can only be mapped post-Build against `IEndpointRouteBuilder`. Hence the DI-stash pattern.

### Key Components

**`HostApplicationBuilderExtensions`** (`Extensions/Hosting/`)
- Namespace `Microsoft.Extensions.Hosting` (convention: extension methods placed in the framework namespace they extend, so consumers get them for free).
- `RegisterIdentityProvider<TRegistrar, TSettings, TInstanceSettings>()` — the one public entry point.

**`IdentityProviderMapping`** (root)
- Namespace `Cirreum.Identity` — joins the Identity family's three-namespace convention (`Cirreum.Identity`, `Cirreum.Identity.Configuration`, `Cirreum.Identity.Provisioning`).
- Public record — Runtime Extensions packages need to resolve `IEnumerable<IdentityProviderMapping>` from DI, so the type must be public.

### Project Structure

```
src/Cirreum.Runtime.IdentityProvider/
├── Extensions/
│   └── Hosting/
│       └── HostApplicationBuilderExtensions.cs   # RegisterIdentityProvider<>
├── IdentityProviderMapping.cs                    # DI-stashed deferred-map delegate
└── Cirreum.Runtime.IdentityProvider.csproj
```

## Configuration Pattern

Config path: `Cirreum:{ProviderType}:Providers:{ProviderName}` — matches AuthZ. `ProviderType` is always `"Identity"` (from `ProviderType.Identity`). `ProviderName` comes from the concrete registrar's override (`"Oidc"`, `"EntraExternalId"`).

## Dependencies

- **Cirreum.IdentityProvider** — base registrar + settings + provisioning contracts
- **Cirreum.Logging.Deferred** — startup diagnostics
- **Microsoft.AspNetCore.App** — `IEndpointRouteBuilder`

## Build Commands

```bash
dotnet build Cirreum.Runtime.IdentityProvider.slnx
dotnet pack --configuration Release
```

## Development Notes

- Uses .NET 10.0 with latest C# language version
- Nullable reference types enabled
- `RootNamespace` = `Cirreum.Runtime` (AuthZ parallel), but extension class uses `Microsoft.Extensions.Hosting` namespace explicitly
- File-scoped namespaces
- K&R braces, tabs (matches repo `.editorconfig`)
- Marker-type + deferred-logger patterns come from `Cirreum.Providers` / `Cirreum.Logging.Deferred`
