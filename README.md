# Cirreum Runtime IdentityProvider

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.IdentityProvider/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.IdentityProvider/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.IdentityProvider/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Runtime.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Runtime.IdentityProvider/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Runtime-layer registration helper for the Cirreum Identity provider family.**

## Overview

`Cirreum.Runtime.IdentityProvider` is the Runtime-layer library that bootstraps any `IdentityProviderRegistrar<TSettings, TInstanceSettings>` from configuration. Single responsibility: given a registrar type, bind the corresponding config section and call both phases of the registrar's lifecycle correctly.

Apps do **not** reference this package directly — they install a Runtime Extensions package such as `Cirreum.Runtime.Identity.Oidc`, `Cirreum.Runtime.Identity.EntraExternalId`, or the umbrella `Cirreum.Runtime.Identity`, which use this helper internally.

## Two-phase registration recap

Identity provider registrars run in two phases:

1. **Services phase** (before `builder.Build()`) — `Register(settings, services, configuration)` wires up DI.
2. **Endpoints phase** (after `builder.Build()`) — `Map(settings, endpoints)` maps HTTP routes.

`IEndpointRouteBuilder` isn't available at builder time, so this helper runs phase 1 immediately and **stashes a closure** for phase 2 as a DI singleton. The Runtime Extensions layer pulls the stashed closures at `Map*Identity()` / `MapIdentity()` call time and invokes them against the live `IEndpointRouteBuilder`.

## API

### `RegisterIdentityProvider<TRegistrar, TSettings, TInstanceSettings>()`

```csharp
using Microsoft.Extensions.Hosting;

builder.RegisterIdentityProvider<
    OidcIdentityProviderRegistrar,
    OidcIdentityProviderSettings,
    OidcIdentityProviderInstanceSettings>();
```

Generally called from inside per-protocol Runtime Extension packages (`AddOidcIdentity()`, `AddEntraExternalIdIdentity()`), not from app code.

**What it does:**

1. Dedup check via marker-type registration — repeated calls for the same `TRegistrar` are no-ops.
2. Binds `Cirreum:Identity:Providers:{ProviderName}` from `IConfiguration` to `TSettings`.
3. Skips with a debug log if the section is missing (or throws if `required: true` was passed).
4. Calls `registrar.Register(providerSettings, services, configuration)` — services phase.
5. Registers an `IdentityProviderMapping` in DI capturing a closure over `registrar.Map(providerSettings, endpoints)` — deferred endpoints phase.

### `IdentityProviderMapping` (stashed in DI)

```csharp
public sealed record IdentityProviderMapping(
    string ProviderName,
    Action<IEndpointRouteBuilder> Map);
```

Runtime Extensions packages resolve `IEnumerable<IdentityProviderMapping>` at `Map*Identity()` time:
- The **umbrella** `MapIdentity()` invokes every registered mapping.
- A **per-protocol** `MapOidcIdentity()` filters by `ProviderName == "Oidc"` and invokes just those.

## Dependencies

- **Cirreum.IdentityProvider** — base registrar, settings, provisioning contracts
- **Cirreum.Logging.Deferred** — deferred logging for startup diagnostics
- **Microsoft.AspNetCore.App** — `IEndpointRouteBuilder`

## Versioning

Follows [Semantic Versioning](https://semver.org/). Foundational library — major bumps are rare and coordinated with `Cirreum.IdentityProvider` releases.

## License

MIT — see [LICENSE](LICENSE).

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*
