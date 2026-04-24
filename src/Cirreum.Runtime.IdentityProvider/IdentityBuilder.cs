namespace Cirreum.Identity;

using Cirreum.Identity.Provisioning;
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

		hostBuilder.Services.AddKeyedScoped<IUserProvisioner, TProvisioner>(instanceKey);
		return this;
	}
}
