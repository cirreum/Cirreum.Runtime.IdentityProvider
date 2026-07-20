namespace Cirreum.Runtime.IdentityProvider.Tests;

using Cirreum.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Unit tests for <c>RegisterIdentityProvider&lt;,,&gt;()</c>: config-driven bootstrap,
/// the silent skip on missing configuration, instance-record stashing (enabled and
/// disabled), the deferred endpoints-phase mapping, and registrar-type dedup.
/// </summary>
/// <remarks>
/// Instance keys are unique per test — the registration path is exercised repeatedly
/// across tests and instance keys must never collide between compositions.
/// </remarks>
public class RegisterIdentityProviderTests {

	[Fact]
	public void MissingSection_SkipsSilently() {
		var builder = CreateBuilder();

		builder.RegisterIdentityProvider<TestIdentityRegistrar, TestProviderSettings, TestInstanceSettings>();

		Instances(builder.Services).Should().BeEmpty();
		Mappings(builder.Services).Should().BeEmpty();
		ProvisionerMarkers(builder.Services).Should().BeEmpty();
	}

	[Fact]
	public void MissingSection_WhenRequired_Throws() {
		var builder = CreateBuilder();

		var act = () => builder
			.RegisterIdentityProvider<TestIdentityRegistrar, TestProviderSettings, TestInstanceSettings>(required: true);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Cirreum:Identity:Providers:TestIdentity*");
	}

	[Fact]
	public void SectionPresent_RegistersEnabledInstances_AndStashesAllInstanceRecords() {
		var builder = CreateBuilder(
			("rip-enabled-1a", true),
			("rip-disabled-1b", false));

		builder.RegisterIdentityProvider<TestIdentityRegistrar, TestProviderSettings, TestInstanceSettings>();

		// Records exist for BOTH instances — disabled included — so the orphan check
		// can distinguish "unknown key" from "known but disabled".
		var records = Instances(builder.Services);
		records.Should().HaveCount(2);
		records.Single(r => r.InstanceKey == "rip-enabled-1a").Enabled.Should().BeTrue();
		records.Single(r => r.InstanceKey == "rip-disabled-1b").Enabled.Should().BeFalse();
		records.Should().OnlyContain(r => r.ProviderName == "TestIdentity");

		// Services-phase registration ran for the enabled instance only.
		ProvisionerMarkers(builder.Services).Should().ContainSingle()
			.Which.Key.Should().Be("rip-enabled-1a");

		// The deferred endpoints-phase mapping is stashed once, under the provider name.
		Mappings(builder.Services).Should().ContainSingle()
			.Which.ProviderName.Should().Be("TestIdentity");
	}

	[Fact]
	public void DuplicateRegistrarType_SecondCallIsSkipped() {
		var builder = CreateBuilder(("rip-dedup-2a", true));

		builder.RegisterIdentityProvider<TestIdentityRegistrar, TestProviderSettings, TestInstanceSettings>();
		builder.RegisterIdentityProvider<TestIdentityRegistrar, TestProviderSettings, TestInstanceSettings>();

		Instances(builder.Services).Should().HaveCount(1);
		Mappings(builder.Services).Should().HaveCount(1);
		ProvisionerMarkers(builder.Services).Should().HaveCount(1);
	}

	private static HostApplicationBuilder CreateBuilder(params (string Key, bool Enabled)[] instances) {
		var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
		var config = new Dictionary<string, string?>();
		foreach (var (key, enabled) in instances) {
			var prefix = $"Cirreum:Identity:Providers:TestIdentity:Instances:{key}";
			config[$"{prefix}:Enabled"] = enabled ? "true" : "false";
			config[$"{prefix}:Route"] = $"/hook/{key}";
		}
		builder.Configuration.AddInMemoryCollection(config);
		return builder;
	}

	private static List<IdentityInstanceRegistration> Instances(IServiceCollection services) =>
		[.. services.Select(d => d.ImplementationInstance).OfType<IdentityInstanceRegistration>()];

	private static List<IdentityProviderMapping> Mappings(IServiceCollection services) =>
		[.. services.Select(d => d.ImplementationInstance).OfType<IdentityProviderMapping>()];

	private static List<RegisteredProvisionerMarker> ProvisionerMarkers(IServiceCollection services) =>
		[.. services.Select(d => d.ImplementationInstance).OfType<RegisteredProvisionerMarker>()];

}
