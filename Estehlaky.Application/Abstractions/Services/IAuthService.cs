using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;

namespace Estehlaky.Application.Abstractions.Services
{
    /// <summary>
    /// Defines the application-level contract for authentication and identity management.
    /// </summary>
    /// <remarks>
    /// This abstraction provides a clean boundary between the Application and Infrastructure
    /// layers. Its implementation is responsible for handling identity operations using
    /// ASP.NET Core Identity and issuing JWT-based authentication tokens.
    ///
    /// No ASP.NET Core Identity or infrastructure-specific types should be exposed
    /// through this interface.
    /// </remarks>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user account and establishes an authenticated session.
        /// </summary>
        /// <param name="email">The email address used as the user's login identifier.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="fullName">The user's full name, if provided.</param>
        /// <param name="phone">The user's phone number, if provided.</param>
        /// <param name="language">The user's preferred application language.</param>
        /// <param name="client">Information about the client device and connection.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result containing the authentication response, including
        /// access and refresh tokens, and the user's profile; otherwise, a failure
        /// result describing the registration error.
        /// </returns>
        Task<Result<AuthResponseDto>> RegisterAsync(
            string email,
            string password,
            string? fullName,
            string? phone,
            string language,
            ClientInfo client,
            CancellationToken ct);

        /// <summary>
        /// Updates the authenticated user's profile information.
        /// </summary>
        /// <param name="userId">The unique identifier for the user.</param>
        /// <param name="fullName">The user's updated full name, if provided.</param>
        /// <param name="phoneNumber">The user's updated phone number, if provided.</param>
        /// <param name="profilePictureUrl">The user's updated profile picture URL, if provided.</param>
        /// <param name="language">The user's preferred application language, if provided.</param>
        /// <param name="client">Information about the client device and connection.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result containing the updated user profile;
        /// otherwise, a failure result describing the update error.
        /// </returns>
        Task<Result<UserDto>> EditProfileAsync(
            Guid userId,
            string? fullName,
            string? phoneNumber,
            string? profilePictureUrl,
            string? language,
            ClientInfo client,
            CancellationToken ct);

        /// <summary>
        /// Authenticates an existing user using their email address and password.
        /// </summary>
        /// <param name="email">The email address associated with the account.</param>
        /// <param name="phone">The phone number associated with the account</param>
        /// <param name="password">The user's current password.</param>
        /// <param name="client">Information about the client device and connection.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result containing authentication tokens and the user's profile;
        /// otherwise, a failure result indicating invalid credentials, account lockout,
        /// or another authentication error.
        /// </returns>
        Task<Result<AuthResponseDto>> LoginAsync(
            string email,
            string? phone,
            string password,
            ClientInfo client,
            CancellationToken ct);

        /// <summary>
        /// Exchanges a valid refresh token for a new authentication session.
        /// </summary>
        /// <remarks>
        /// Refresh token rotation is applied to reduce the risk of token theft.
        /// Previously used tokens are monitored for reuse, and suspicious reuse
        /// may result in revocation of the entire token family.
        /// </remarks>
        /// <param name="refreshToken">The refresh token presented by the client.</param>
        /// <param name="client">Information about the client device and connection.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result containing a new access token and refresh token;
        /// otherwise, a failure result indicating an invalid, expired, revoked,
        /// or reused refresh token.
        /// </returns>
        Task<Result<AuthResponseDto>> RefreshAsync(
            string refreshToken,
            ClientInfo client,
            CancellationToken ct);

        /// <summary>
        /// Signs out the current session by revoking the refresh token's family.
        /// </summary>
        /// <remarks>
        /// This operation is idempotent. If the supplied refresh token does not exist,
        /// the operation still completes successfully.
        /// </remarks>
        /// <param name="refreshToken">The refresh token associated with the session to revoke.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the session has been revoked or the token
        /// does not exist; otherwise, a failure result.
        /// </returns>
        Task<Result> LogoutAsync(
            string refreshToken,
            CancellationToken ct);

        /// <summary>
        /// Signs out a user from all active sessions across all devices.
        /// </summary>
        /// <remarks>
        /// All active refresh tokens associated with the specified user are revoked,
        /// requiring the user to authenticate again on every device.
        /// </remarks>
        /// <param name="userId">The unique identifier of the user to sign out.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>A successful result when the revocation operation completes.</returns>
        Task<Result> LogoutAllAsync(
            Guid userId,
            CancellationToken ct);

        /// <summary>
        /// Initiates the password recovery process for an existing user account.
        /// </summary>
        /// <remarks>
        /// A password reset token is generated and sent to the user's registered
        /// email address. The operation intentionally returns success even when
        /// the email address is not associated with an account to prevent
        /// user enumeration.
        /// </remarks>
        /// <param name="email">The email address associated with the account.</param>
        /// <param name="phone">The user's phone number, if provided, for additional verification.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the recovery request is processed;
        /// otherwise, a failure result if the operation cannot be completed.
        /// </returns>
        Task<Result> ForgotPasswordAsync(
            string email,
            string? phone,
            CancellationToken ct);

        /// <summary>
        /// Resets a user's password using a valid password reset token.
        /// </summary>
        /// <remarks>
        /// Upon successful password reset, all active refresh tokens are revoked,
        /// requiring the user to sign in again on all devices. Any account lockout
        /// state and failed access attempt counters are also cleared.
        /// </remarks>
        /// <param name="email">The email address associated with the account.</param>
        /// <param name="phone">The user's phone number, if provided, for additional verification.</param>
        /// <param name="otp">The password reset OTP received by the user.</param>
        /// <param name="newPassword">The new password to assign to the account.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the password is reset;
        /// otherwise, a failure result describing an invalid token or validation error.
        /// </returns>
        Task<Result> ResetPasswordAsync(
            string email,
            string? phone,
            string otp,
            string newPassword,
            CancellationToken ct);

        /// <summary>
        /// Changes the password of an authenticated user.
        /// </summary>
        /// <remarks>
        /// The current password must be valid. Once the password is changed,
        /// all active refresh tokens are revoked, requiring the user to
        /// authenticate again on every device.
        /// </remarks>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <param name="currentPassword">The user's existing password.</param>
        /// <param name="newPassword">The new password to assign to the account.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the password is changed;
        /// otherwise, a failure result describing an invalid current password
        /// or password validation error.
        /// </returns>
        Task<Result> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken ct);


        /// <summary>
        /// Retrieves the profile information of a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result containing the user's profile information;
        /// otherwise, a failure result if the user does not exist or is unavailable.
        /// </returns>
        Task<Result<UserDto>> GetUserAsync(
            Guid userId,
            CancellationToken ct);

        /// <summary>
        /// Requests email confirmation for a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the email confirmation is requested;
        /// otherwise, a failure result.
        /// </returns>
        Task<Result> RequestEmailConfirmationAsync(
            Guid userId,
            CancellationToken ct);

        /// <summary>
        /// Confirms a user's email address using the OTP sent to that email.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="otp">The verification OTP received by the user.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the email is confirmed;
        /// otherwise, a failure result describing an invalid or expired OTP.
        /// </returns>
        Task<Result> ConfirmEmailAsync(
            Guid userId,
            string otp,
            CancellationToken ct);



        /// <summary>
        /// Requests phone confirmation for a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the phone confirmation is requested;
        /// otherwise, a failure result.
        /// </returns>
        Task<Result> RequestPhoneConfirmationAsync(
            Guid userId,
            CancellationToken ct);

        /// <summary>
        /// Confirms a user's phone number using the OTP sent to that phone.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="otp">The verification OTP received by the user.</param>
        /// <param name="ct">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A successful result when the phone number is confirmed;
        /// otherwise, a failure result describing an invalid or expired OTP.
        /// </returns>
        Task<Result> ConfirmPhoneAsync(
            Guid userId,
            string otp,
            CancellationToken ct);
    }
}
