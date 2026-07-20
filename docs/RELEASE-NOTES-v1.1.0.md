# Cirreum.Runtime.IdentityProvider 1.1.0 — Orphaned provisioners fail loudly

`AddProvisioner<T>(instanceKey)` now verifies its instance key against the configured
identity provider instances at composition time. A provisioner keyed to an instance
that doesn't exist is unreachable — previously that misconfiguration produced a bare
404 on the provisioning callback route with zero startup signal; now it stops the
host with a message that names the fix.

Strictly additive for correctly-configured applications.

---

## Why this release exists

Identity providers deliberately skip silently when their configuration section is
missing — correct for umbrella packages, where most apps configure a subset of the
composed providers. But that silence also swallowed a whole class of drift: an app
whose composition *names an instance key* (`AddProvisioner<MyProvisioner>("descope")`)
while its configuration no longer has a matching instance — after a config-shape
migration, a typo, or an environment overlay gone wrong. Both registrars would skip
at Debug, `MapIdentity()` would map nothing, and the IdP's callback would 404 with no
explanation anywhere. The composition callsite held the evidence of intent the whole
time; the framework just never cross-checked it.

## What's new

**The orphan check.** Provider registration now records every configured instance —
enabled or not — and `AddProvisioner` validates its key against that set at call time:

- **No match at all** → a deferred **Warning**, which fails the host at startup
  validation. The message names the provisioner type, the orphaned key, every
  configured instance key, and the expected configuration shape — a typo is a
  ten-second fix instead of a debugging session.
- **Only disabled matches** → a deferred **Information** advisory. Registering a
  provisioner for an instance that's disabled in this environment is legitimate
  (dev without the IdP, staged rollouts) and stays non-fatal.
- **An enabled match** → silence, as before.

Key matching is case-sensitive, deliberately: the instance key doubles as the
provisioner's keyed-DI key, which resolves ordinally — a casing mismatch is a real
misconfiguration.

```csharp
builder.AddIdentity(id => id
    .AddProvisioner<LapCastUserProvisioner>("descope"));
// With no configured instance named "descope", startup now fails:
//   AddProvisioner<LapCastUserProvisioner>("descope") does not match any configured
//   identity provider instance — the provisioner can never be invoked. Configured
//   instance keys: (none). Verify the instance key and the configuration shape
//   (Cirreum:Identity:Providers:<ProviderName>:Instances:<key>).
```

## Compatibility

- **Additive.** Correctly-configured applications see no behavior change.
- **A latent misconfiguration now fails at startup.** An application currently
  registering a provisioner whose key matches nothing was already broken — its
  callback route didn't exist — it just didn't know. That app now fails to boot with
  the actionable message instead. To keep a provisioner registered for an instance an
  environment doesn't use, declare the instance in configuration with
  `"Enabled": false` — that's the Information path.
- No new dependencies.

## See also

- `Cirreum.Runtime.Authentication` 1.2.0 — the companion diagnostics for the
  authentication track's silent no-match failure mode (unmapped JWT audiences)
- `Cirreum.IdentityProvider` 1.0.8 — collection-scoped duplicate-instance guard
