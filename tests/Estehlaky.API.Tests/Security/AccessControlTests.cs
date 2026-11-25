using System.Net;
using Estehlaky.API.Tests.Support;
using Estehlaky.Application.Me;
using Estehlaky.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Estehlaky.API.Tests.Security;

/// <summary>
/// The backend rule: userA must never reach user B's data. Today the only user-scoped endpoint is /me,
/// so these tests also lock down authentication itself. When property endpoints arrive, add the
/// "userA reads userB's property -> 404/403" tests here.
/// </summary>
[Collection(ApiCollection.Name)]
public class AccessControlTests(ApiFixture api) : ApiTestBase(api)
{
    // ------------------------------------------------------------------ who am I

    [Fact]
    public async Task Me_returns_the_profile_of_the_token_owner_and_nobody_else()
    {
        var alice = await Client.NewAccountAsync();
        var bob = await Client.NewAccountAsync();

        var aliceMe = await Client.GetMeAsync(alice.AccessToken);
        var bobMe = await Client.GetMeAsync(bob.AccessToken);

        Assert.Equal(alice.UserId, aliceMe.Id);
        Assert.Equal(alice.Email, aliceMe.Email);
        Assert.Equal(bob.UserId, bobMe.Id);
        Assert.NotEqual(aliceMe.Id, bobMe.Id);
    }

    [Fact]
    public async Task Me_cannot_be_pointed_at_another_user_through_the_query_string()
    {
        var alice = await Client.NewAccountAsync();
        var bob = await Client.NewAccountAsync();

        var response = await Client.GetAsync($"/api/v1/me?userId={bob.UserId}&id={bob.UserId}", alice.AccessToken);

        await response.ExpectAsync(HttpStatusCode.OK);
        Assert.Equal(alice.UserId, (await response.ReadAsync<UserDto>()).Id);
    }

    // ------------------------------------------------------------------ anonymous callers

    [Theory]
    [InlineData("GET", "/api/v1/me")]
    [InlineData("POST", "/api/v1/auth/logout-all")]
    [InlineData("POST", "/api/v1/auth/change-password")]
    public async Task Protected_endpoints_reject_anonymous_callers(string method, string url)
    {
        object? body = method == "POST"
            ? new { currentPassword = TestConfig.Password, newPassword = TestConfig.OtherPassword }
            : null;

        var response = await Client.SendAsync(new HttpMethod(method), url, body);

        await response.ExpectAsync(HttpStatusCode.Unauthorized);
    }

    // ------------------------------------------------------------------ bad tokens
    // The first test is the control: if hand-built tokens were never accepted, the rejection tests below would prove nothing.

    [Fact]
    public async Task A_hand_built_token_with_the_right_key_issuer_and_audience_is_accepted()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId);

        Assert.Equal(account.UserId, (await Client.GetMeAsync(token)).Id);
    }

    [Fact]
    public async Task A_garbage_token_is_rejected()
    {
        var response = await Client.MeAsync("not.a.jwt");

        await response.ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_signed_with_a_different_key_is_rejected()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId, key: "a-completely-different-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz");

        await (await Client.MeAsync(token)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_with_the_wrong_issuer_is_rejected()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId, issuer: "someone-else");

        await (await Client.MeAsync(token)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_with_the_wrong_audience_is_rejected()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId, audience: "another-app");

        await (await Client.MeAsync(token)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId, lifetime: TimeSpan.FromHours(-1));

        await (await Client.MeAsync(token)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_signed_with_an_algorithm_other_than_HS256_is_rejected()
    {
        var account = await Client.NewAccountAsync();

        var token = TestTokens.Create(account.UserId, algorithm: SecurityAlgorithms.HmacSha512);

        await (await Client.MeAsync(token)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_valid_token_for_a_user_that_does_not_exist_is_rejected()
    {
        var token = TestTokens.Create(Guid.NewGuid());

        var response = await Client.MeAsync(token);

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.UserNotFound");
    }

    // ------------------------------------------------------------------ deleted accounts

    [Fact]
    public async Task A_soft_deleted_user_cannot_sign_in_refresh_or_read_their_profile()
    {
        var account = await Client.NewAccountAsync();
        await SoftDeleteAsync(account.Email);

        await (await Client.LoginAsync(account.Email, null, account.Password))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        await (await Client.RefreshAsync(account.RefreshToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
        await (await Client.MeAsync(account.AccessToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.UserNotFound");
    }

    private async Task SoftDeleteAsync(string email)
    {
        using var scope = Api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updated = await db.Users
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, (DateTimeOffset?)DateTimeOffset.UtcNow));

        Assert.Equal(1, updated);
    }
}
