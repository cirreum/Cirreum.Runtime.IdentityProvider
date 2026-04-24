namespace Cirreum.Identity;

using Microsoft.AspNetCore.Routing;

/// <summary>
/// Per-provider endpoint-mapping delegate stashed in DI during
/// <c>RegisterIdentityProvider&lt;,,&gt;()</c> (the services phase) for deferred execution
/// by <c>MapIdentity()</c> / per-protocol <c>Map*Identity()</c> extensions in the Runtime
/// Extensions layer (the endpoints phase, after <c>builder.Build()</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProviderName"/> is the registrar's <c>ProviderName</c> (e.g. <c>"Oidc"</c>,
/// <c>"EntraExternalId"</c>). Consumers that only want to map a single protocol can filter
/// the <see cref="IEnumerable{T}"/> resolved from DI on this property; the umbrella
/// <c>MapIdentity()</c> invokes every registered mapping.
/// </para>
/// <para>
/// <see cref="Map"/> is a closure over the registrar instance and its bound provider
/// settings — calling it on an <see cref="IEndpointRouteBuilder"/> walks the enabled
/// instances and maps each one's callback route.
/// </para>
/// </remarks>
/// <param name="ProviderName">The registrar's <c>ProviderName</c>.</param>
/// <param name="Map">Deferred endpoint-mapping delegate.</param>
public sealed record IdentityProviderMapping(
	string ProviderName,
	Action<IEndpointRouteBuilder> Map);
