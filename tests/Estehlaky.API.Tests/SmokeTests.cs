using System.Net;
using Estehlaky.API.Tests.Support;
using Estehlaky.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.API.Tests;

/// <summary>Cheap checks that the app starts, migrates, seeds and documents itself.</summary>
[Collection(ApiCollection.Name)]
public class SmokeTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task The_openapi_document_is_generated()
    {
        var response = await Client.GetAsync("/openapi/v1.json");

        await response.ExpectAsync(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Estehlaky API", json);
        Assert.Contains("/api/v1/auth/login", json);
    }

    [Fact]
    public async Task Migrations_ran_and_the_seeders_created_the_roles()
    {
        using var scope = Api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync();
        var roles = await db.Roles.Select(r => r.Name).ToListAsync();

        Assert.Empty(pending);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task An_unknown_route_asks_anonymous_callers_for_a_token()
    {
        // The API answers 401 Auth.TokenRequired for every path outside its public allow-list, even unknown ones.
        var response = await Client.GetAsync("/api/v1/does-not-exist");

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.TokenRequired");
    }

    [Fact]
    public async Task An_unknown_route_is_a_404_for_a_signed_in_user()
    {
        var account = await Client.NewAccountAsync();

        var response = await Client.GetAsync("/api/v1/does-not-exist", account.AccessToken);

        await response.ExpectAsync(HttpStatusCode.NotFound);
    }
}
