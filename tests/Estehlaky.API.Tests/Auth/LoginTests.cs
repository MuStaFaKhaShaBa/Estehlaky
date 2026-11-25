using System.Net;
using Estehlaky.API.Tests.Support;

namespace Estehlaky.API.Tests.Auth;

[Collection(ApiCollection.Name)]
public class LoginTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Login_with_the_right_credentials_returns_tokens_and_the_profile()
    {
        var account = await Client.NewAccountAsync();

        var auth = await Client.SignInAsync(account.Email, null, account.Password);

        Assert.Equal(account.UserId, auth.User.Id);
        Assert.NotEqual(account.RefreshToken, auth.RefreshToken);   // every login starts a new session
    }

    [Fact]
    public async Task Login_ignores_the_letter_case_of_the_email()
    {
        var account = await Client.NewAccountAsync();

        var auth = await Client.SignInAsync(account.Email.ToUpperInvariant(), null, account.Password);

        Assert.Equal(account.UserId, auth.User.Id);
    }

    [Fact]
    public async Task A_wrong_password_returns_401_InvalidCredentials()
    {
        var account = await Client.NewAccountAsync();

        var response = await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd");

        await response.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
    }

    [Fact]
    public async Task An_unknown_email_gives_the_same_answer_as_a_wrong_password()
    {
        var account = await Client.NewAccountAsync();

        var wrongPassword = await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd");
        var unknownEmail = await Client.LoginAsync(TestConfig.NewEmail(), null, TestConfig.Password);

        var a = await wrongPassword.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        var b = await unknownEmail.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        Assert.Equal(a.Detail, b.Detail);   // emails cannot be discovered by comparing responses
    }

    [Fact]
    public async Task Empty_credentials_return_400_with_field_errors()
    {
        var response = await Client.LoginAsync("", null,    "");

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "email");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "password");
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        var account = await Client.NewAccountAsync();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var failed = await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd");
            await failed.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidCredentials");
        }

        var fifth = await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd");
        await fifth.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.LockedOut");

        var correctButLocked = await Client.LoginAsync(account.Email, null, account.Password);
        await correctButLocked.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.LockedOut");
    }

    [Fact]
    public async Task A_successful_login_resets_the_failed_attempt_counter()
    {
        var account = await Client.NewAccountAsync();

        for (var round = 0; round < 3; round++)
        {
            for (var attempt = 0; attempt < 3; attempt++)
                await (await Client.LoginAsync(account.Email, null, "Wrong!Passw0rd"))
                    .ExpectAsync(HttpStatusCode.Unauthorized);

            await Client.SignInAsync(account.Email, null, account.Password);   // resets the counter
        }

        // 9 failures in total, but never 5 in a row: still not locked.
        await Client.SignInAsync(account.Email, null,       account.Password);
    }

    [Fact]
    public async Task Only_the_newest_ten_sessions_stay_active()
    {
        var account = await Client.NewAccountAsync();   // session 1
        for (var i = 0; i < 10; i++)
            await Client.SignInAsync(account.Email, null, account.Password);   // sessions 2..11

        var oldest = await Client.RefreshAsync(account.RefreshToken);

        await oldest.ExpectProblemAsync(HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }
}
