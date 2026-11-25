using Estehlaky.Application.Auth.ChangePassword;
using Estehlaky.Application.Auth.ForgotPassword;
using Estehlaky.Application.Auth.Login;
using Estehlaky.Application.Auth.Logout;
using Estehlaky.Application.Auth.Refresh;
using Estehlaky.Application.Auth.Register;
using Estehlaky.Application.Auth.ResetPassword;
using FluentValidation.TestHelper;

namespace Estehlaky.UnitTests.Auth;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterCommand Valid(
        string email = "user@test.local",string phone= "1234567890", string password = "Str0ng!Passw0rd",
        string? fullName = "Ahmed", string language = "en") =>
        new(email, password, fullName, phone, language, TestData.Client);

    [Fact]
    public void A_complete_request_is_valid() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Full_name_is_optional() =>
        _validator.TestValidate(Valid(fullName: null)).ShouldNotHaveValidationErrorFor(x => x.FullName);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Bad_emails_are_rejected(string email) =>
        _validator.TestValidate(Valid(email: email)).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void An_email_longer_than_256_characters_is_rejected() =>
        _validator.TestValidate(Valid(email: new string('a', 250) + "@test.local"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Passwords_shorter_than_8_are_rejected(string password) =>
        _validator.TestValidate(Valid(password: password)).ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void A_password_longer_than_128_characters_is_rejected() =>
        _validator.TestValidate(Valid(password: new string('a', 129))).ShouldHaveValidationErrorFor(x => x.Password);

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Passwords_at_the_length_limits_pass_the_validator(int length) =>
        _validator.TestValidate(Valid(password: new string('a', length))).ShouldNotHaveValidationErrorFor(x => x.Password);

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Supported_languages_are_accepted(string language) =>
        _validator.TestValidate(Valid(language: language)).ShouldNotHaveValidationErrorFor(x => x.Language);

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData("EN")]
    public void Other_languages_are_rejected(string language) =>
        _validator.TestValidate(Valid(language: language)).ShouldHaveValidationErrorFor(x => x.Language);

    [Fact]
    public void A_full_name_longer_than_200_characters_is_rejected() =>
        _validator.TestValidate(Valid(fullName: new string('x', 201))).ShouldHaveValidationErrorFor(x => x.FullName);
}

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Email_and_password_are_enough() =>
        _validator.TestValidate(new LoginCommand("a@b.co",null, "x", TestData.Client)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_fields_are_rejected()
    {
        var result = _validator.TestValidate(new LoginCommand("", "","", TestData.Client));

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Login_does_not_enforce_password_rules_so_old_passwords_still_work() =>
        _validator.TestValidate(new LoginCommand("a@b.co",null, "short", TestData.Client))
            .ShouldNotHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void A_password_longer_than_128_characters_is_rejected() =>
        _validator.TestValidate(new LoginCommand("a@b.co",null, new string('a', 129), TestData.Client))
            .ShouldHaveValidationErrorFor(x => x.Password);
}

public class RefreshAndLogoutValidatorTests
{
    [Theory]
    [InlineData("")]
    public void Refresh_needs_a_token(string token) =>
        new RefreshValidator().TestValidate(new RefreshCommand(token, TestData.Client))
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);

    [Fact]
    public void Refresh_rejects_tokens_over_512_characters() =>
        new RefreshValidator().TestValidate(new RefreshCommand(new string('t', 513), TestData.Client))
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);

    [Fact]
    public void Refresh_accepts_a_normal_token() =>
        new RefreshValidator().TestValidate(new RefreshCommand(new string('t', 86), TestData.Client))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Logout_needs_a_token() =>
        new LogoutValidator().TestValidate(new LogoutCommand(""))
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);

    [Fact]
    public void Logout_accepts_a_normal_token() =>
        new LogoutValidator().TestValidate(new LogoutCommand("abc")).ShouldNotHaveAnyValidationErrors();
}

public class PasswordFlowValidatorTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("nope", false)]
    [InlineData("user@test.local", true)]
    public void Forgot_password_validates_the_email(string email, bool valid)
    {
        var result = new ForgotPasswordValidator().TestValidate(new ForgotPasswordCommand(email, null));

        if (valid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Reset_password_needs_email_token_and_a_valid_new_password()
    {
        var result = new ResetPasswordValidator().TestValidate(new ResetPasswordCommand("",null, "", "short"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Token);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Reset_password_accepts_a_complete_request() =>
        new ResetPasswordValidator().TestValidate(new ResetPasswordCommand("a@b.co",null, "token", "Str0ng!Passw0rd"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Change_password_needs_the_current_password() =>
        new ChangePasswordValidator().TestValidate(new ChangePasswordCommand("", "Str0ng!Passw0rd"))
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);

    [Fact]
    public void Change_password_rejects_a_new_password_equal_to_the_current_one() =>
        new ChangePasswordValidator().TestValidate(new ChangePasswordCommand("Str0ng!Passw0rd", "Str0ng!Passw0rd"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void Change_password_accepts_a_different_new_password() =>
        new ChangePasswordValidator().TestValidate(new ChangePasswordCommand("Str0ng!Passw0rd", "An0ther!Passw0rd"))
            .ShouldNotHaveAnyValidationErrors();
}
