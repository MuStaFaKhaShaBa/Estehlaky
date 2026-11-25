using Estehlaky.Application.Common.Results;

namespace Estehlaky.Application.Auth;

/// <summary>Codes are a public contract: the mobile app localizes by code. Never rename a shipped one.</summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid email or password.", ErrorType.Unauthorized);

    public static readonly Error LockedOut =
        new("Auth.LockedOut", "Too many failed attempts. Try again later.", ErrorType.Unauthorized);

    public static readonly Error EmailTaken =
        new("Auth.EmailTaken", "This email is already registered.", ErrorType.Conflict);

    public static readonly Error InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "The refresh token is invalid or expired.", ErrorType.Unauthorized);

    public static readonly Error RefreshTokenReused =
        new("Auth.RefreshTokenReused", "This refresh token was already used. Please sign in again.", ErrorType.Unauthorized);

    public static readonly Error InvalidResetToken =
        new("Auth.InvalidResetToken", "The password reset token is invalid or expired.", ErrorType.Validation);

    public static readonly Error InvalidResetOtp =
        new("Auth.InvalidResetOtp", "The password reset OTP is invalid or expired.", ErrorType.Validation);

    public static readonly Error WrongCurrentPassword =
        new("Auth.WrongCurrentPassword", "The current password is incorrect.", ErrorType.Validation);

    public static readonly Error UserNotFound =
        new("Auth.UserNotFound", "The user no longer exists.", ErrorType.Unauthorized);

    public static readonly Error TokenRequired =
    new(
        "Auth.TokenRequired",
        "An access token is required.",
        ErrorType.Unauthorized);

    public static readonly Error InvalidAccessToken =
        new(
            "Auth.InvalidAccessToken",
            "The access token is invalid.",
            ErrorType.Unauthorized);

    public static readonly Error ExpiredAccessToken =
        new(
            "Auth.ExpiredAccessToken",
            "The access token has expired. Please sign in again.",
            ErrorType.Unauthorized);

    public static readonly Error Forbidden =
        new(
            "Auth.Forbidden",
            "You do not have permission to access this resource.",
            ErrorType.Forbidden);

    public static readonly Error ServiceUnavailable =
    new("Auth.ServiceUnavailable", "The service is temporarily unavailable. Please try again in a moment.", ErrorType.Unavailable);

    public static readonly Error ConcurrentRequest =
        new("Auth.ConcurrentRequest", "Another request for this account is in progress. Please try again.", ErrorType.Conflict);

    public static readonly Error DuplicateRecord =
        new("Auth.DuplicateRecord", "A record with the same unique value already exists.", ErrorType.Conflict);

    public static readonly Error DatabaseError =
        new("Auth.DatabaseError", "The data could not be saved.", ErrorType.Failure);

    public static readonly Error Unexpected =
        new("Auth.Unexpected", "An unexpected error occurred. If it continues, contact support and quote the trace id.", ErrorType.Failure);

    public static readonly Error InvalidEmailConfirmationOtp =
    new(
        "Auth.InvalidEmailConfirmationOtp",
        "The email confirmation OTP is invalid or expired.",
        ErrorType.Validation);

    public static readonly Error InvalidPhoneConfirmationOtp =
        new(
            "Auth.InvalidPhoneConfirmationOtp",
            "The phone confirmation OTP is invalid or expired.",
            ErrorType.Validation);

    public static readonly Error EmailAlreadyConfirmed =
        new(
            "Auth.EmailAlreadyConfirmed",
            "The email address is already confirmed.",
            ErrorType.Conflict);

    public static readonly Error PhoneAlreadyConfirmed =
        new(
            "Auth.PhoneAlreadyConfirmed",
            "The phone number is already confirmed.",
            ErrorType.Conflict);

    public static readonly Error InvalidConfirmationOtp =
    new("Auth.InvalidConfirmationOtp", "The confirmation code is invalid or expired.", ErrorType.Validation);
}

