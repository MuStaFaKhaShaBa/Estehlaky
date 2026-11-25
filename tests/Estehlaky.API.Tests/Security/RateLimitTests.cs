using System.Net;
using Estehlaky.API.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Estehlaky.API.Tests.Security;

[Collection(ApiCollection.Name)]
public class RateLimitTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Auth_endpoints_return_429_once_the_per_minute_limit_is_used_up()
    {
        // A second host of the same app with a limit of 3, so the shared host keeps its high limit.
        await using var limited = Api.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:AuthPermitLimit"] = "3"
            })));

        using var client = new ApiClient(limited.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));
        var email = TestConfig.NewEmail();

        for (var i = 0; i < 3; i++)
            await (await client.LoginAsync(email, null, "Wrong!Passw0rd")).ExpectAsync(HttpStatusCode.Unauthorized);

        var blocked = await client.LoginAsync(email, null, "Wrong!Passw0rd");

        await blocked.ExpectAsync(HttpStatusCode.TooManyRequests);

        // The limit belongs to the auth policy only: other endpoints are not affected.
        var me = await client.MeAsync(null);
        await me.ExpectAsync(HttpStatusCode.Unauthorized);
    }
}
