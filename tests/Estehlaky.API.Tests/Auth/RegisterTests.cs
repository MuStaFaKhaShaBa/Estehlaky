using System.Net;
using Estehlaky.API.Contracts;
using Estehlaky.API.Tests.Support;
using Estehlaky.Application.Auth;
using Estehlaky.Domain.Enums;

namespace Estehlaky.API.Tests.Auth;

[Collection(ApiCollection.Name)]
public class RegisterTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Register_creates_the_account_and_returns_a_token_pair_and_profile()
    {
        var email = TestConfig.NewEmail();

        var response = await Client.RegisterAsync(new RegisterRequest(email, TestConfig.Password, "Ahmed Hassan", null, "en", null));

        await response.ExpectAsync(HttpStatusCode.Created);
        var auth = await response.ReadAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.True(auth.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.True(auth.RefreshTokenExpiresAt > auth.AccessTokenExpiresAt);
        Assert.Equal(email, auth.User.Email);
        Assert.Equal("Ahmed Hassan", auth.User.FullName);
        Assert.Equal("en", auth.User.PreferredLanguage);
        Assert.Equal(ThemeMode.System, auth.User.Theme);
    }

    [Fact]
    public async Task Register_defaults_the_language_to_Arabic()
    {
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), TestConfig.Password, "Test User", null, null, null));

        await response.ExpectAsync(HttpStatusCode.Created);
        Assert.Equal("ar", (await response.ReadAsync<AuthResponseDto>()).User.PreferredLanguage);
    }

    [Fact]
    public async Task Register_without_a_full_name_never_causes_a_server_error()
    {
        // The request validator treats FullName as optional, but Identity.Users.FullName is NOT NULL,
        // so this currently returns 500 (SqlException: cannot insert NULL into FullName).
        // Fix in the API: either require FullName in the validator (400) or store an empty string.
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), TestConfig.Password, null, null, "en", null));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode < 500, $"Expected 201 or 400 but got {(int)response.StatusCode}. Body: {body}");
    }

    [Fact]
    public async Task Register_trims_the_full_name()
    {
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), TestConfig.Password, "  Omar  ", null, "en", null));

        await response.ExpectAsync(HttpStatusCode.Created);
        Assert.Equal("Omar", (await response.ReadAsync<AuthResponseDto>()).User.FullName);
    }

    [Fact]
    public async Task The_access_token_from_register_works_straight_away()
    {
        var account = await Client.NewAccountAsync();

        var me = await Client.GetMeAsync(account.AccessToken);

        Assert.Equal(account.UserId, me.Id);
        Assert.Equal(account.Email, me.Email);
    }

    [Fact]
    public async Task Registering_an_email_twice_returns_409_EmailTaken()
    {
        var account = await Client.NewAccountAsync();

        var response = await Client.RegisterAsync(new RegisterRequest(account.Email, TestConfig.Password, null, null, "en", null));

        await response.ExpectProblemAsync(HttpStatusCode.Conflict, "Auth.EmailTaken");
    }

    [Fact]
    public async Task Email_uniqueness_ignores_letter_case()
    {
        var account = await Client.NewAccountAsync();

        var response = await Client.RegisterAsync(new RegisterRequest(account.Email.ToUpperInvariant(), TestConfig.Password, null, null, "en", null));

        await response.ExpectProblemAsync(HttpStatusCode.Conflict, "Auth.EmailTaken");
    }

    [Fact]
    public async Task An_invalid_email_returns_400_with_an_email_field_error()
    {
        var response = await Client.RegisterAsync(new RegisterRequest("not-an-email", TestConfig.Password, null, null, "en", null));

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "email");
    }

    [Fact]
    public async Task An_unsupported_language_returns_400_with_a_language_field_error()
    {
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), TestConfig.Password, null, null, "fr", null));

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "language");
    }

    [Theory]
    [InlineData("Aa1!aaaa")]          // 8 characters: passes the request validator, fails the Identity minimum of 10
    [InlineData("alllowercase123")]   // no uppercase letter and no symbol
    public async Task A_password_that_breaks_the_identity_policy_returns_400_with_a_password_field_error(string password)
    {
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), password, null, null, "en", null));

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "password");
    }

    [Fact]
    public async Task A_password_shorter_than_8_is_rejected_by_the_request_validator()
    {
        var response = await Client.RegisterAsync(new RegisterRequest(TestConfig.NewEmail(), "Aa1!a", null, null, "en", null));

        var problem = await response.ExpectProblemAsync(HttpStatusCode.BadRequest, "General.Validation");
        Assert.Contains(problem.FieldErrors!, f => f.Field == "password");
    }
}
