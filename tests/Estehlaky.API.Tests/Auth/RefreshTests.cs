using System.Net;
using Estehlaky.API.Tests.Support;

namespace Estehlaky.API.Tests.Auth;

[Collection(ApiCollection.Name)]
public class RefreshTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Refresh_returns_a_new_token_pair_for_the_same_user()
    {
        var account = await Client.NewAccountAsync();

        var refreshed = await Client.RefreshOkAsync(account.RefreshToken);

        Assert.NotEqual(account.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(account.AccessToken, refreshed.AccessToken);
        Assert.Equal(account.UserId, refreshed.User.Id);
        await Client.GetMeAsync(refreshed.AccessToken);   // the new access token works
    }

    [Fact]
    public async Task Refresh_tokens_rotate_along_a_chain()
    {
        var account = await Client.NewAccountAsync();

        var second = await Client.RefreshOkAsync(account.RefreshToken);
        var third = await Client.RefreshOkAsync(second.RefreshToken);

        Assert.NotEqual(second.RefreshToken, third.RefreshToken);
    }

    [Fact]
    public async Task An_unknown_refresh_token_returns_401_InvalidRefreshToken()
    {
        var response = await Client.RefreshAsync("this-token-was-never-issued");

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task An_empty_refresh_token_returns_400()
    {
        var response = await Client.RefreshAsync("");

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
    }

    [Fact]
    public async Task The_same_token_can_be_retried_inside_the_grace_window()
    {
        var account = await Client.NewAccountAsync();

        await Client.RefreshOkAsync(account.RefreshToken);
        var retry = await Client.RefreshAsync(account.RefreshToken);   // lost response / parallel request

        await retry.ExpectAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reusing_a_token_after_the_grace_window_ends_the_whole_session()
    {
        var account = await Client.NewAccountAsync();
        var rotated = await Client.RefreshOkAsync(account.RefreshToken);

        Api.Clock.Advance(TimeSpan.FromSeconds(11));   // grace window is 10 seconds
        var replay = await Client.RefreshAsync(account.RefreshToken);

        await replay.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.RefreshTokenReused");

        // Theft response: the newer token of the same session is dead too.
        var afterTheft = await Client.RefreshAsync(rotated.RefreshToken);
        await afterTheft.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task A_refresh_token_expires_after_30_days()
    {
        var account = await Client.NewAccountAsync();

        Api.Clock.Advance(TimeSpan.FromDays(31));
        var response = await Client.RefreshAsync(account.RefreshToken);

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task A_session_cannot_be_extended_past_90_days_by_refreshing()
    {
        var account = await Client.NewAccountAsync();
        var token = account.RefreshToken;

        // Refresh every 25 days (inside the 30-day sliding window) until the 90-day cap is reached.
        for (var i = 0; i < 3; i++)
        {
            Api.Clock.Advance(TimeSpan.FromDays(25));
            token = (await Client.RefreshOkAsync(token)).RefreshToken;
        }

        Api.Clock.Advance(TimeSpan.FromDays(25));   // day 100: past the absolute limit
        var response = await Client.RefreshAsync(token);

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }
}
