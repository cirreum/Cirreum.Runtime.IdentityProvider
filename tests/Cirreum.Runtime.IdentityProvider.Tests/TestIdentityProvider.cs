namespace Cirreum.Runtime.IdentityProvider.Tests;

using Cirreum.Identity;
using Cirreum.Identity.Configuration;
using Cirreum.Identity.Provisioning;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Concrete test doubles for exercising the config-driven registration helper and
/// the identity builder. The registrar stashes a <see cref="RegisteredProvisionerMarker"/>
/// per registered instance so tests can observe the helper's internally-constructed
/// registrar doing its services-phase work.
/// </summary>
internal sealed record RegisteredProvisionerMarker(string Key);

internal sealed class TestInstanceSettings : IdentityProviderInstanceSettings {
}

internal sealed class TestProviderSettings : IdentityProviderSettings<TestInstanceSettings> {
}

internal sealed class TestIdentityRegistrar : IdentityProviderRegistrar<TestProviderSettings, TestInstanceSettings> {

	public override string ProviderName => "TestIdentity";

	protected override void RegisterProvisioner(
		string key,
		TestInstanceSettings settings,
		IServiceCollection services,
		IConfiguration configuration) {
		services.AddSingleton(new RegisteredProvisionerMarker(key));
	}

	protected override void MapProvisioner(
		string key,
		TestInstanceSettings settings,
		IEndpointRouteBuilder endpoints) {
	}

}

internal sealed class TestProvisioner : IUserProvisioner {

	public Task<ProvisionResult> ProvisionAsync(
		ProvisionContext context,
		CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("Composition-only test double.");

}
