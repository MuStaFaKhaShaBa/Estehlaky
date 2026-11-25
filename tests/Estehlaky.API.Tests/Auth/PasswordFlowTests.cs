using System.Net;
using Estehlaky.API.Contracts;
using Estehlaky.API.Tests.Support;

namespace Estehlaky.API.Tests.Auth;

[Collection(ApiCollection.Name)]
public class PasswordFlowTests(ApiFixture api) : ApiTestBase(api)
{
    private Task<HttpResponseMessage> ForgotAsync(string email) =>
        Client.PostAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email, null));

    private Task<HttpResponseMessage> ResetAsync(string email, string token, string newPassword) =>
        Client.PostAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(email, null, token, newPassword));

    private Task<HttpResponseMessage> ChangeAsync(string? accessToken, string current, string next) =>
        Client.PostAsync("/api/v1/auth/change-password", new ChangePasswordRequest(current, next), accessToken);

    private async Task<string> RequestResetTokenAsync(string email)
    {
        await (await ForgotAsync(email)).ExpectAsync(HttpStatusCode.OK);
        var sent = Api.Emails.LastOtpFor(email);
        Assert.NotNull(sent);
        return sent!.Otp;
    }

    // ------------------------------------------------------------------ forgot password

    [Fact]
    public async Task Forgot_password_for_a_registered_email_sends_a_reset_token()
    {
        var account = await Client.NewAccountAsync(language: "en");

        await RequestResetTokenAsync(account.Email);

        var sent = Api.Emails.LastOtpFor(account.Email)!;
        Assert.False(string.IsNullOrWhiteSpace(sent.Otp));
        Assert.Equal("en", sent.Language);
    }

    [Fact]
    public async Task Forgot_password_for_an_unknown_email_answers_200_and_sends_nothing()
    {
        var unknown = TestConfig.NewEmail();

        var response = await ForgotAsync(unknown);

        await response.ExpectAsync(HttpStatusCode.OK);
        Assert.Null(Api.Emails.LastOtpFor(unknown));
    }

    [Fact]
    public async Task Forgot_password_with_an_invalid_email_returns_400()
    {
        var response = await ForgotAsync("not-an-email");

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
    }

    // ------------------------------------------------------------------ reset password

    [Fact]
    public async Task Reset_password_changes_the_password_and_signs_every_device_out()
    {
        var account = await Client.NewAccountAsync();
        var token = await RequestResetTokenAsync(account.Email);

        await (await ResetAsync(account.Email, token, TestConfig.OtherPassword)).ExpectAsync(HttpStatusCode.OK);

        await (await Client.LoginAsync(account.Email, null, account.Password))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        await Client.SignInAsync(account.Email, null, TestConfig.OtherPassword);
        await (await Client.RefreshAsync(account.RefreshToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task A_reset_token_works_only_once()
    {
        var account = await Client.NewAccountAsync();
        var token = await RequestResetTokenAsync(account.Email);
        await (await ResetAsync(account.Email, token, TestConfig.OtherPassword)).ExpectAsync(HttpStatusCode.OK);

        var replay = await ResetAsync(account.Email, token, "Third!Passw0rd1");

        await replay.ExpectProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidResetToken");
    }

    [Fact]
    public async Task A_made_up_reset_token_returns_400_InvalidResetToken()
    {
        var account = await Client.NewAccountAsync();

        var response = await ResetAsync(account.Email, "made-up-token", TestConfig.OtherPassword);

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidResetToken");
    }

    [Fact]
    public async Task Reset_for_an_unknown_email_gives_the_same_error_as_a_bad_token()
    {
        var response = await ResetAsync(TestConfig.NewEmail(), "made-up-token", TestConfig.OtherPassword);

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidResetToken");
    }

    [Fact]
    public async Task A_reset_token_cannot_be_used_for_another_account()
    {
        var alice = await Client.NewAccountAsync();
        var bob = await Client.NewAccountAsync();
        var aliceToken = await RequestResetTokenAsync(alice.Email);

        var response = await ResetAsync(bob.Email, aliceToken, TestConfig.OtherPassword);

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "Auth.InvalidResetToken");
        await Client.SignInAsync(bob.Email, null, bob.Password);   // Bob's password is unchanged
    }

    [Fact]
    public async Task Reset_password_enforces_the_password_policy()
    {
        var account = await Client.NewAccountAsync();
        var token = await RequestResetTokenAsync(account.Email);

        var response = await ResetAsync(account.Email, token, "alllowercase123");

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "password");
    }

    [Fact]
    public async Task Reset_password_clears_a_lockout()
    {
        var account = await Client.NewAccountAsync();
        for (var i = 0; i < 5; i++)
            await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd");
        await (await Client.LoginAsync(account.Email, null, account.Password))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.LockedOut");

        var token = await RequestResetTokenAsync(account.Email);
        await (await ResetAsync(account.Email, token, TestConfig.OtherPassword)).ExpectAsync(HttpStatusCode.OK);

        await Client.SignInAsync(account.Email, null, TestConfig.OtherPassword);
    }

    // ------------------------------------------------------------------ change password

    [Fact]
    public async Task Change_password_updates_the_password_and_signs_every_device_out()
    {
        var account = await Client.NewAccountAsync();

        await (await ChangeAsync(account.AccessToken, account.Password, TestConfig.OtherPassword))
            .ExpectAsync(HttpStatusCode.OK);

        await (await Client.LoginAsync(account.Email, null, account.Password))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        await Client.SignInAsync(account.Email, null, TestConfig.OtherPassword);
        await (await Client.RefreshAsync(account.RefreshToken))
            .ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Change_password_with_a_wrong_current_password_returns_400_WrongCurrentPassword()
    {
        var account = await Client.NewAccountAsync();

        var response = await ChangeAsync(account.AccessToken, "Wrong!Passw0rd", TestConfig.OtherPassword);

        await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "Auth.WrongCurrentPassword");
    }

    [Fact]
    public async Task Change_password_rejects_a_new_password_equal_to_the_current_one()
    {
        var account = await Client.NewAccountAsync();

        var response = await ChangeAsync(account.AccessToken, account.Password, account.Password);

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "newPassword");
    }

    [Fact]
    public async Task Change_password_enforces_the_password_policy()
    {
        var account = await Client.NewAccountAsync();

        var response = await ChangeAsync(account.AccessToken, account.Password, "alllowercase123");

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "password");
    }

    [Fact]
    public async Task Change_password_without_a_token_returns_401()
    {
        var response = await ChangeAsync(null, TestConfig.Password, TestConfig.OtherPassword);

        await response.ExpectAsync(HttpStatusCode.Unauthorized);
    }
}
