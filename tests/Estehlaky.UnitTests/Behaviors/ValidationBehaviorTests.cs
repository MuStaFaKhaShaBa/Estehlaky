using Estehlaky.Application.Auth;
using Estehlaky.Application.Auth.Login;
using Estehlaky.Application.Behaviors;
using Estehlaky.Application.Common.Results;
using FluentValidation;
using MediatR;

namespace Estehlaky.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private static LoginCommand Command(string email, string? phone, string password) =>
        new(email, phone, password, TestData.Client);

    private static ValidationBehavior<LoginCommand, Result<AuthResponseDto>> Behavior(params IValidator<LoginCommand>[] validators) =>
        new(validators);

    [Fact]
    public async Task Without_validators_the_request_goes_straight_to_the_handler()
    {
        var expected = Result.Success(TestData.AuthResponse());
        var called = false;
        RequestHandlerDelegate<Result<AuthResponseDto>> next = _ => { called = true; return Task.FromResult(expected); };

        var result = await Behavior().Handle(Command("", null, ""), next, CancellationToken.None);

        Assert.True(called);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task A_valid_request_reaches_the_handler()
    {
        var expected = Result.Success(TestData.AuthResponse());
        var called = false;
        RequestHandlerDelegate<Result<AuthResponseDto>> next = _ => { called = true; return Task.FromResult(expected); };

        var result = await Behavior(new LoginValidator()).Handle(Command("a@b.co", null, "pw"), next, CancellationToken.None);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task An_invalid_request_is_stopped_before_the_handler_and_returns_a_validation_error()
    {
        var called = false;
        RequestHandlerDelegate<Result<AuthResponseDto>> next = _ => { called = true; return Task.FromResult(Result.Success(TestData.AuthResponse())); };

        var result = await Behavior(new LoginValidator()).Handle(Command("", null, ""), next, CancellationToken.None);

        Assert.False(called);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains(result.Error.Fields!, f => f.Field == "email");
        Assert.Contains(result.Error.Fields!, f => f.Field == "password");
    }
}
