using Estehlaky.Application.Auth;
using Estehlaky.Application.Common.Results;

namespace Estehlaky.UnitTests.Auth;

/// <summary>
/// Error codes are a public contract: the mobile app translates messages by code.
/// If one of these tests fails, a shipped code or its HTTP meaning was changed.
/// </summary>
public class AuthErrorsContractTests
{
    [Theory]
    [MemberData(nameof(Contract))]
    public void Code_and_type_are_stable(Error error, string code, ErrorType type)
    {
        Assert.Equal(code, error.Code);
        Assert.Equal(type, error.Type);
    }

    public static TheoryData<Error, string, ErrorType> Contract => new()
    {
        { AuthErrors.InvalidCredentials, "Auth.InvalidCredentials", ErrorType.Unauthorized },
        { AuthErrors.LockedOut, "Auth.LockedOut", ErrorType.Unauthorized },
        { AuthErrors.EmailTaken, "Auth.EmailTaken", ErrorType.Conflict },
        { AuthErrors.InvalidRefreshToken, "Auth.InvalidRefreshToken", ErrorType.Unauthorized },
        { AuthErrors.RefreshTokenReused, "Auth.RefreshTokenReused", ErrorType.Unauthorized },
        { AuthErrors.InvalidResetToken, "Auth.InvalidResetToken", ErrorType.Validation },
        { AuthErrors.WrongCurrentPassword, "Auth.WrongCurrentPassword", ErrorType.Validation },
        { AuthErrors.UserNotFound, "Auth.UserNotFound", ErrorType.Unauthorized },
    };
}
