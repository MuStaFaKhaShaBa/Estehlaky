using Estehlaky.API.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.API.Tests.Security;

/// <summary>
/// The API is "secure by default" (a fallback policy requires a signed-in user). This test fails the build
/// when someone adds [AllowAnonymous] to a new endpoint, so every public endpoint is a conscious decision.
/// </summary>
[Collection(ApiCollection.Name)]
public class EndpointGuardTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly string[] ExpectedAnonymous =
    [
        "api/v1/auth/register",
        "api/v1/auth/login",
        "api/v1/auth/refresh",
        "api/v1/auth/logout",
        "api/v1/auth/forgot-password",
        "api/v1/auth/reset-password"
    ];

    private static readonly string[] MustBeProtected =
    [
        "api/v1/me",
        "api/v1/auth/logout-all",
        "api/v1/auth/change-password"
    ];

    private List<RouteEndpoint> ApiEndpoints() =>
        Api.Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

    private static string Route(RouteEndpoint e) => e.RoutePattern.RawText!.TrimStart('/').ToLowerInvariant();

    [Fact]
    public void Only_the_expected_api_endpoints_allow_anonymous_access()
    {
        var anonymous = ApiEndpoints()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(Route)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal(ExpectedAnonymous.Order().ToArray(), anonymous);
    }

    [Fact]
    public void Sensitive_endpoints_are_not_anonymous()
    {
        var endpoints = ApiEndpoints();

        foreach (var route in MustBeProtected)
        {
            var matches = endpoints.Where(e => Route(e) == route).ToList();
            Assert.NotEmpty(matches);
            Assert.All(matches, e => Assert.Null(e.Metadata.GetMetadata<IAllowAnonymous>()));
        }
    }
}
