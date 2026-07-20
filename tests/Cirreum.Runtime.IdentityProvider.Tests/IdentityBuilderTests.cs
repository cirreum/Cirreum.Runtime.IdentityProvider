namespace Cirreum.Runtime.IdentityProvider.Tests;

using Cirreum.Identity;
using Cirreum.Identity.Provisioning;
using Cirreum.Logging.Deferred;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Unit tests for <see cref="IdentityBuilder"/>: the orphaned-provisioner check
/// (unmatched key → deferred Warning; disabled-only match → deferred Information;
/// enabled match → silent) and the keyed provisioner registration itself.
/// </summary>
/// <remarks>
/// The deferred log queue is process-global with no reset API, so every test uses a
/// globally-unique instance key and asserts on entries mentioning that key only.
/// </remarks>
public class IdentityBuilderTests {

	[Fact]
	public void AddProvisioner_UnmatchedKey_LogsDeferredWarning_AndStillRegisters() {
		const string key = "ib-orphan-77f1";
		var builder = CreateBuilder();
		var identity = new IdentityBuilder(builder);

		identity.AddProvisioner<TestProvisioner>(key);

		WarningsMentioning(key).Should().ContainSingle()
			.Which.Should().Contain(nameof(TestProvisioner))
			.And.Contain("(none)");
		HasKeyedProvisioner(builder.Services, key).Should().BeTrue();
	}

	[Fact]
	public void AddProvisioner_UnmatchedKey_WarningNamesTheKnownKeys() {
		const string key = "ib-orphan-88a2";
		const string known = "ib-known-88a2";
		var builder = CreateBuilder();
		builder.Services.AddSingleton(new IdentityInstanceRegistration("TestIdentity", known, Enabled: true));
		var identity = new IdentityBuilder(builder);

		identity.AddProvisioner<TestProvisioner>(key);

		WarningsMentioning(key).Should().ContainSingle()
			.Which.Should().Contain(known);
	}

	[Fact]
	public void AddProvisioner_DisabledOnlyMatch_LogsInformation_NotWarning() {
		const string key = "ib-disabled-99b3";
		var builder = CreateBuilder();
		builder.Services.AddSingleton(new IdentityInstanceRegistration("TestIdentity", key, Enabled: false));
		var identity = new IdentityBuilder(builder);

		identity.AddProvisioner<TestProvisioner>(key);

		WarningsMentioning(key).Should().BeEmpty();
		InformationMentioning(key).Should().ContainSingle()
			.Which.Should().Contain("TestIdentity");
		HasKeyedProvisioner(builder.Services, key).Should().BeTrue();
	}

	[Fact]
	public void AddProvisioner_EnabledMatch_LogsNothing() {
		const string key = "ib-enabled-aa04";
		var builder = CreateBuilder();
		builder.Services.AddSingleton(new IdentityInstanceRegistration("TestIdentity", key, Enabled: true));
		var identity = new IdentityBuilder(builder);

		identity.AddProvisioner<TestProvisioner>(key);

		WarningsMentioning(key).Should().BeEmpty();
		InformationMentioning(key).Should().BeEmpty();
		HasKeyedProvisioner(builder.Services, key).Should().BeTrue();
	}

	[Fact]
	public void AddProvisioner_KeyMatching_IsCaseSensitive() {
		// The instance key doubles as the keyed-DI key, which resolves ordinally —
		// a casing mismatch is a real misconfiguration and must warn.
		const string key = "ib-Case-bb15";
		var builder = CreateBuilder();
		builder.Services.AddSingleton(new IdentityInstanceRegistration("TestIdentity", key.ToLowerInvariant(), Enabled: true));
		var identity = new IdentityBuilder(builder);

		identity.AddProvisioner<TestProvisioner>(key);

		WarningsMentioning(key).Should().ContainSingle();
	}

	[Fact]
	public void AddProvisioner_ReturnsSelf_ForChaining() {
		const string key = "ib-chain-cc26";
		var builder = CreateBuilder();
		builder.Services.AddSingleton(new IdentityInstanceRegistration("TestIdentity", key, Enabled: true));
		var identity = new IdentityBuilder(builder);

		var result = identity.AddProvisioner<TestProvisioner>(key);

		result.Should().BeSameAs(identity);
	}

	private static HostApplicationBuilder CreateBuilder() =>
		Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

	private static bool HasKeyedProvisioner(IServiceCollection services, string key) =>
		services.Any(d => d.ServiceType == typeof(IUserProvisioner)
			&& d.IsKeyedService
			&& Equals(d.ServiceKey, key));

	private static List<string> WarningsMentioning(string key) =>
		[.. Logger.GetAll(LogLevel.Warning).Select(e => e.Message).Where(m => m.Contains(key))];

	private static List<string> InformationMentioning(string key) =>
		[.. Logger.GetAll(LogLevel.Information).Select(e => e.Message).Where(m => m.Contains(key))];

}
