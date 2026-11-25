using System.Net;
using Estehlaky.API.Tests.Support;

namespace Estehlaky.API.Tests.Auth;

[Collection(ApiCollection.Name)]
public class LogoutTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Logout_ends_the_session()
    {
        var account = await Client.NewAccountAsync();

        var logout = await Client.LogoutAsync(account.RefreshToken);
        await logout.ExpectAsync(HttpStatusCode.OK);

        var refresh = await Client.RefreshAsync(account.RefreshToken);
        await refresh.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Logout_ends_the_whole_session_family_not_just_the_token_that_was_sent()
    {
        var account = await Client.NewAccountAsync();
        var rotated = await Client.RefreshOkAsync(account.RefreshToken);

        await (await Client.LogoutAsync(account.RefreshToken)).ExpectAsync(HttpStatusCode.OK);

        var refresh = await Client.RefreshAsync(rotated.RefreshToken);
        await refresh.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Logout_only_affects_the_device_that_sent_the_token()
    {
        var account = await Client.NewAccountAsync();
        var otherDevice = await Client.SignInAsync(account.Email, null, account.Password);

        await (await Client.LogoutAsync(account.RefreshToken)).ExpectAsync(HttpStatusCode.OK);

        await Client.RefreshOkAsync(otherDevice.RefreshToken);
    }

    [Fact]
    public async Task Logout_with_an_unknown_token_still_succeeds()
    {
        var response = await Client.LogoutAsync("this-token-was-never-issued");

        await response.ExpectAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_needs_no_access_token()
    {
        var account = await Client.NewAccountAsync();

        var response = await Client.PostAsync("/api/v1/auth/logout", new { refreshToken = account.RefreshToken }, bearer: null);

        await response.ExpectAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_all_ends_every_session_of_the_user()
    {
        var account = await Client.NewAccountAsync();
        var second = await Client.SignInAsync(account.Email, null, account.Password);

        var response = await Client.LogoutAllAsync(account.AccessToken);
        await response.ExpectAsync(HttpStatusCode.OK);

        await (await Client.RefreshAsync(account.RefreshToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
        await (await Client.RefreshAsync(second.RefreshToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Logout_all_never_touches_the_sessions_of_other_users()
    {
        var alice = await Client.NewAccountAsync();
        var bob = await Client.NewAccountAsync();

        await (await Client.LogoutAllAsync(alice.AccessToken)).ExpectAsync(HttpStatusCode.OK);

        await Client.RefreshOkAsync(bob.RefreshToken);   // Bob is still signed in
    }

    [Fact]
    public async Task Logout_all_without_a_token_returns_401()
    {
        var response = await Client.LogoutAllAsync(null);

        await response.ExpectAsync(HttpStatusCode.Unauthorized);
    }
}
