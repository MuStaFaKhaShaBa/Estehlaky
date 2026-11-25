using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Auth.ChangePassword;
using Estehlaky.Application.Auth.ForgotPassword;
using Estehlaky.Application.Auth.Login;
using Estehlaky.Application.Auth.Logout;
using Estehlaky.Application.Auth.LogoutAll;
using Estehlaky.Application.Auth.Refresh;
using Estehlaky.Application.Auth.Register;
using Estehlaky.Application.Auth.ResetPassword;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;
using Estehlaky.Application.Me.Get;
using NSubstitute;

namespace Estehlaky.UnitTests.Auth;

public class AuthHandlerTests
{
    private readonly IAuthService _auth = Substitute.For<IAuthService>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly CancellationToken _ct = CancellationToken.None;

    // ---------------------------------------------------------------- delegating handlers

    [Fact]
    public async Task Register_passes_every_field_to_the_auth_service()
    {
        var expected = Result.Success(TestData.AuthResponse());
        _auth.RegisterAsync("a@b.co", "pw", "Name", "1234567890", "ar", TestData.Client, _ct).Returns(expected);

        var result = await new RegisterHandler(_auth)
            .Handle(new RegisterCommand("a@b.co", "pw", "Name", "123456", "ar", TestData.Client), _ct);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Login_passes_credentials_and_client_to_the_auth_service()
    {
        var expected = Result.Success(TestData.AuthResponse());
        _auth.LoginAsync("a@b.co",null, "pw", TestData.Client, _ct).Returns(expected);

        var result = await new LoginHandler(_auth).Handle(new LoginCommand("a@b.co",null, "pw", TestData.Client), _ct);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Login_returns_the_failure_of_the_auth_service_unchanged()
    {
        _auth.LoginAsync(default!,null, default!, default!, default).ReturnsForAnyArgs(
            Result.Failure<AuthResponseDto>(AuthErrors.InvalidCredentials));

        var result = await new LoginHandler(_auth).Handle(new LoginCommand("a@b.co",null, "bad", TestData.Client), _ct);

        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Refresh_passes_the_token_to_the_auth_service()
    {
        var expected = Result.Success(TestData.AuthResponse());
        _auth.RefreshAsync("token", TestData.Client, _ct).Returns(expected);

        var result = await new RefreshHandler(_auth).Handle(new RefreshCommand("token", TestData.Client), _ct);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Logout_passes_the_token_to_the_auth_service()
    {
        _auth.LogoutAsync("token", _ct).Returns(Result.Success());

        var result = await new LogoutHandler(_auth).Handle(new LogoutCommand("token"), _ct);

        Assert.True(result.IsSuccess);
        await _auth.Received(1).LogoutAsync("token", _ct);
    }

    [Fact]
    public async Task ForgotPassword_passes_the_email_to_the_auth_service()
    {
        _auth.ForgotPasswordAsync("a@b.co", null, _ct).Returns(Result.Success());

        var result = await new ForgotPasswordHandler(_auth).Handle(new ForgotPasswordCommand("a@b.co", null), _ct);

        Assert.True(result.IsSuccess);
        await _auth.Received(1).ForgotPasswordAsync("a@b.co", null, _ct);
    }

    [Fact]
    public async Task ResetPassword_passes_email_token_and_new_password()
    {
        _auth.ResetPasswordAsync("a@b.co", null, "tok", "NewPassw0rd!", _ct).Returns(Result.Success());

        var result = await new ResetPasswordHandler(_auth)
            .Handle(new ResetPasswordCommand("a@b.co", null, "tok", "NewPassw0rd!"), _ct);

        Assert.True(result.IsSuccess);
        await _auth.Received(1).ResetPasswordAsync("a@b.co", null, "tok", "NewPassw0rd!", _ct);
    }

    // ---------------------------------------------------------------- handlers that need a signed-in user

    [Fact]
    public async Task ChangePassword_uses_the_current_user_id()
    {
        var userId = Guid.NewGuid();
        _current.Id.Returns((Guid?)userId);
        _auth.ChangePasswordAsync(userId, "old", "new", _ct).Returns(Result.Success());

        var result = await new ChangePasswordHandler(_auth, _current)
            .Handle(new ChangePasswordCommand("old", "new"), _ct);

        Assert.True(result.IsSuccess);
        await _auth.Received(1).ChangePasswordAsync(userId, "old", "new", _ct);
    }

    [Fact]
    public async Task ChangePassword_without_a_user_is_unauthorized_and_never_reaches_the_service()
    {
        var result = await new ChangePasswordHandler(_auth, _current)
            .Handle(new ChangePasswordCommand("old", "new"), _ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        _ = _auth.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default, default!, default!, default);
    }

    [Fact]
    public async Task LogoutAll_uses_the_current_user_id()
    {
        var userId = Guid.NewGuid();
        _current.Id.Returns((Guid?)userId);
        _auth.LogoutAllAsync(userId, _ct).Returns(Result.Success());

        var result = await new LogoutAllHandler(_auth, _current).Handle(new LogoutAllCommand(), _ct);

        Assert.True(result.IsSuccess);
        await _auth.Received(1).LogoutAllAsync(userId, _ct);
    }

    [Fact]
    public async Task LogoutAll_without_a_user_is_unauthorized_and_never_reaches_the_service()
    {
        var result = await new LogoutAllHandler(_auth, _current).Handle(new LogoutAllCommand(), _ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        _ = _auth.DidNotReceiveWithAnyArgs().LogoutAllAsync(default, default);
    }

    [Fact]
    public async Task GetMe_returns_the_profile_of_the_current_user()
    {
        var userId = Guid.NewGuid();
        var dto = TestData.User(userId);
        _current.Id.Returns((Guid?)userId);
        _auth.GetUserAsync(userId, _ct).Returns(Result.Success(dto));

        var result = await new GetMeHandler(_auth, _current).Handle(new GetMeQuery(), _ct);

        Assert.Equal(dto, result.Value);
    }

    [Fact]
    public async Task GetMe_without_a_user_is_unauthorized_and_never_reaches_the_service()
    {
        var result = await new GetMeHandler(_auth, _current).Handle(new GetMeQuery(), _ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        _ = _auth.DidNotReceiveWithAnyArgs().GetUserAsync(default, default);
    }
}
