namespace Microsoft.Extensions.Hosting;

using Cirreum.Identity;
using Cirreum.Identity.Configuration;
using Cirreum.Logging.Deferred;
using Cirreum.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Config-driven registration helpers for the Cirreum Identity provider family.
/// </summary>
public static class HostApplicationBuilderExtensions {

	/// <summary>
	/// Register an Identity provider's registrar: instantiates <typeparamref name="TRegistrar"/>,
	/// binds its settings from <c>Cirreum:Identity:Providers:{ProviderName}</c>, runs the
	/// registrar's services-phase registration, and stashes an <see cref="IdentityProviderMapping"/>
	/// in DI so the Runtime Extensions layer can invoke the endpoints-phase registration after
	/// <c>builder.Build()</c>.
	/// </summary>
	/// <typeparam name="TRegistrar">The identity provider registrar type.</typeparam>
	/// <typeparam name="TSettings">The provider settings type.</typeparam>
	/// <typeparam name="TInstanceSettings">The provider instance settings type.</typeparam>
	/// <param name="builder">The host application builder.</param>
	/// <param name="required">If <see langword="true"/>, throws when the configuration section is missing. Defaults to <see langword="false"/>.</param>
	/// <returns>The host application builder for chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="required"/> is <see langword="true"/> and the configuration
	/// section is missing, or when the section exists but cannot be bound to
	/// <typeparamref name="TSettings"/>.
	/// </exception>
	public static IHostApplicationBuilder RegisterIdentityProvider<TRegistrar, TSettings, TInstanceSettings>(
		this IHostApplicationBuilder builder,
		bool required = false)
		where TRegistrar : IdentityProviderRegistrar<TSettings, TInstanceSettings>, new()
		where TSettings : IdentityProviderSettings<TInstanceSettings>
		where TInstanceSettings : IdentityProviderInstanceSettings {

		var registrarName = typeof(TRegistrar).Name;
		var deferredLogger = Logger.CreateDeferredLogger();

		using (var loggingScope = deferredLogger.BeginScope(new { RegistrarName = registrarName })) {

			// Dedup: if this registrar type has already been wired up, skip.
			if (builder.Services.IsMarkerTypeRegistered<TRegistrar>()) {
				deferredLogger.LogDebug(
					"Duplicate request for {RegistrarName} and will be skipped.",
					registrarName);
				return builder;
			}

			builder.Services.MarkTypeAsRegistered<TRegistrar>();

			var registrar = new TRegistrar();
			var providerSectionKey = GetProviderConfigPath(registrar.ProviderType, registrar.ProviderName);
			var providerSection = builder.Configuration.GetSection(providerSectionKey);
			if (!providerSection.Exists()) {
				if (required) {
					throw new InvalidOperationException(
						$"Configuration required but not found for '{registrarName}' at '{providerSectionKey}'.");
				}

				deferredLogger.LogDebug(
					"Skipping '{RegistrarName}' — no configuration found at '{ConfigPath}'.",
					registrarName,
					providerSectionKey);
				return builder;
			}

			var providerSettings = providerSection.Get<TSettings>()
				?? throw new InvalidOperationException(
					$"Invalid configuration for '{registrarName}' — section exists but cannot be bound to settings.");

			if (providerSettings.Instances.Count == 0) {
				deferredLogger.LogWarning(
					"No instances found to register for {RegistrarName}.",
					registrarName);
				return builder;
			}

			// Record every configured instance (enabled or not) so AddProvisioner can
			// verify its instance key against the composed set — an unmatched key is a
			// misconfiguration that would otherwise surface as a silent 404/500.
			foreach (var (instanceKey, instanceSettings) in providerSettings.Instances) {
				builder.Services.AddSingleton(new IdentityInstanceRegistration(
					registrar.ProviderName,
					instanceKey,
					instanceSettings.Enabled));
			}

			// Services-phase registration: let the registrar wire up per-instance DI (keyed
			// validators, handlers, etc.) using the bound settings.
			registrar.Register(
				providerSettings,
				builder.Services,
				builder.Configuration);

			// Endpoints-phase registration is deferred — IEndpointRouteBuilder isn't
			// available until after builder.Build(). Stash a closure that MapIdentity()
			// (or per-protocol Map*Identity()) can invoke later.
			builder.Services.AddSingleton(new IdentityProviderMapping(
				registrar.ProviderName,
				endpoints => registrar.Map(providerSettings, endpoints)));

			deferredLogger.LogDebug(
				"Registered {InstanceCount} provider instance(s) for {RegistrarName} of type {ProviderType}.",
				providerSettings.Instances.Count,
				registrarName,
				registrar.ProviderType);
		}

		return builder;
	}

	// Helper method for building provider configuration paths.
	private static string GetProviderConfigPath(ProviderType providerType, string providerName) =>
		$"Cirreum:{providerType}:Providers:{providerName}";
}
