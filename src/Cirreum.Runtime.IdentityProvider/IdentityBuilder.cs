namespace Cirreum.Identity;

using Cirreum.Identity.Provisioning;
using Cirreum.Logging.Deferred;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Default <see cref="IIdentityBuilder"/> implementation. Instantiated by the Runtime
/// Extensions layer inside <c>AddIdentity(configure)</c> and passed to the caller's
/// configuration callback.
/// </summary>
public sealed class IdentityBuilder(IHostApplicationBuilder hostBuilder) : IIdentityBuilder {

	/// <inheritdoc />
	public IHostApplicationBuilder HostBuilder => hostBuilder;

	/// <inheritdoc />
	public IIdentityBuilder AddProvisioner<TProvisioner>(string instanceKey)
		where TProvisioner : class, IUserProvisioner {

		// Provider registrars run before this callback, so the configured instance set
		// is already recorded. A provisioner keyed to an instance that doesn't exist is
		// unreachable — fail the host with an actionable message rather than letting the
		// misconfiguration surface as an unmapped callback route.
		var instances = hostBuilder.Services
			.Select(descriptor => descriptor.ImplementationInstance)
			.OfType<IdentityInstanceRegistration>()
			.ToList();
		var matches = instances
			.Where(instance => string.Equals(instance.InstanceKey, instanceKey, StringComparison.Ordinal))
			.ToList();

		var deferredLogger = Logger.CreateDeferredLogger();
		if (matches.Count == 0) {
			deferredLogger.LogWarning(
				"AddProvisioner<{Provisioner}>(\"{InstanceKey}\") does not match any configured identity " +
				"provider instance — the provisioner can never be invoked. Configured instance keys: {KnownKeys}. " +
				"Verify the instance key and the configuration shape " +
				"(Cirreum:Identity:Providers:{{ProviderName}}:Instances:{{key}}).",
				typeof(TProvisioner).Name,
				instanceKey,
				instances.Count == 0 ? "(none)" : string.Join(", ", instances.Select(i => $"'{i.InstanceKey}'")));
		} else if (matches.TrueForAll(instance => !instance.Enabled)) {
			deferredLogger.LogInformation(
				"AddProvisioner<{Provisioner}>(\"{InstanceKey}\") matches only disabled provider instance(s) " +
				"({Providers}) — the provisioner is registered but dormant in this environment.",
				typeof(TProvisioner).Name,
				instanceKey,
				string.Join(", ", matches.Select(m => m.ProviderName)));
		}

		hostBuilder.Services.AddKeyedScoped<IUserProvisioner, TProvisioner>(instanceKey);
		return this;
	}
}
