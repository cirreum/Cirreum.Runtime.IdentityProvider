namespace Cirreum.Identity;

using Cirreum.Identity.Provisioning;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Fluent configuration builder for the Cirreum Identity family. Passed into the callback
/// argument of the Runtime Extensions layer's <c>AddIdentity(configure)</c> extensions.
/// </summary>
/// <remarks>
/// <para>
/// The builder scopes per-instance provisioner registration to a clear site in
/// <c>Program.cs</c>:
/// </para>
/// <code>
/// builder.AddIdentity(p => p
///     .AddProvisioner&lt;ClientABorrowerProvisioner&gt;("clientA_descope")
///     .AddProvisioner&lt;ClientBBorrowerProvisioner&gt;("clientB_descope"));
/// </code>
/// <para>
/// For advanced scenarios, consumers can access the underlying
/// <see cref="IHostApplicationBuilder"/> via <see cref="HostBuilder"/>.
/// </para>
/// </remarks>
public interface IIdentityBuilder {

	/// <summary>
	/// Gets the underlying host application builder.
	/// </summary>
	IHostApplicationBuilder HostBuilder { get; }

	/// <summary>
	/// Registers an application-provided <see cref="IUserProvisioner"/> implementation as a
	/// keyed scoped service, bound to the instance key configured under
	/// <c>Cirreum:Identity:Providers:{ProviderName}:Instances:{instanceKey}</c>.
	/// </summary>
	/// <typeparam name="TProvisioner">
	/// The concrete provisioner type. Must implement <see cref="IUserProvisioner"/>. Typically
	/// derives from <see cref="InvitationUserProvisionerBase{TUser}"/> or
	/// <see cref="SelfServiceUserProvisionerBase{TUser}"/>.
	/// </typeparam>
	/// <param name="instanceKey">
	/// The provisioning instance key — matches the configured instance name in
	/// <c>appsettings.json</c> and becomes <c>ProvisionContext.Source</c> at callback time.
	/// The identity provider handler resolves the keyed <see cref="IUserProvisioner"/> by
	/// this key when the IdP's webhook fires.
	/// </param>
	/// <returns>The builder for fluent chaining.</returns>
	IIdentityBuilder AddProvisioner<TProvisioner>(string instanceKey)
		where TProvisioner : class, IUserProvisioner;
}
