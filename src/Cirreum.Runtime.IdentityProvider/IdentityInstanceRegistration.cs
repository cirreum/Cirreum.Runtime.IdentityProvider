namespace Cirreum.Identity;

/// <summary>
/// Service-collection-scoped record of a configured identity provider instance —
/// one per instance found in configuration, enabled or not. Stashed by
/// <c>RegisterIdentityProvider&lt;,,&gt;()</c> so <see cref="IdentityBuilder.AddProvisioner{TProvisioner}"/>
/// can verify at composition time that the instance key a provisioner is registered
/// under actually exists (and is enabled) across the composed providers.
/// </summary>
/// <param name="ProviderName">The contributing provider's name (e.g. <c>"Oidc"</c>, <c>"EntraExternalId"</c>).</param>
/// <param name="InstanceKey">The configuration instance key (= the provisioner's keyed-DI key and <c>ProvisionContext.Source</c>).</param>
/// <param name="Enabled">Whether the instance is enabled in configuration.</param>
internal sealed record IdentityInstanceRegistration(
	string ProviderName,
	string InstanceKey,
	bool Enabled);
