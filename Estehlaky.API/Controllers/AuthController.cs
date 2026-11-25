
using Estehlaky.API.Contracts;
using Estehlaky.API.Extensions;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Auth.ChangePassword;
using Estehlaky.Application.Auth.ConfirmEmail;
using Estehlaky.Application.Auth.ConfirmPhone;
using Estehlaky.Application.Auth.EditProfile;
using Estehlaky.Application.Auth.ForgotPassword;
using Estehlaky.Application.Auth.Login;
using Estehlaky.Application.Auth.Logout;
using Estehlaky.Application.Auth.LogoutAll;
using Estehlaky.Application.Auth.Refresh;
using Estehlaky.Application.Auth.Register;
using Estehlaky.Application.Auth.ResetPassword;
using Estehlaky.Application.Me;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Estehlaky.API.Controllers
{
    /// <summary>
    /// Sign-up, sign-in and session management using JWT access tokens and rotating refresh tokens.
    /// </summary>
    /// <remarks>
    /// Errors are returned as RFC 7807 application/problem+json with a stable code
    /// (for example Auth.InvalidCredentials) and a traceId.
    /// Clients should translate messages by code, never by the English text.
    /// </remarks>
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public class AuthController(ISender sender) : ControllerBase
    {
        /// <summary>Creates a new account and signs the user in.</summary>
        /// <remarks>
        /// Password rules: 8 to 128 characters. Supported languages: ar (default) and en.
        /// The response contains the same token pair as login, so no second call is needed.
        /// </remarks>
        /// <param name="request">Email, password and optional profile data.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="201">Account created. Returns the standard ApiResponse containing tokens and profile.</response>
        /// <response code="400">Validation failed. See fieldErrors in the response.</response>
        /// <response code="409">The email is already registered (Auth.EmailTaken).</response>
        /// <response code="429">Too many requests. Limit: 10 per minute per IP.</response>
        [HttpPost("register")]
        [AllowAnonymous, EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct=default) =>
            (await sender.Send(
                new RegisterCommand(
                    request.Email,
                    request.Password,
                    request.FullName,
                    request.Phone,
                    request.Language ?? "ar",
                    Client(request.DeviceId)), ct))
            .ToActionResult(this, statusCode: StatusCodes.Status201Created, message:"User Created Successfully");


        /// <summary>Updates the authenticated user's profile information.</summary>
        /// <remarks>
        /// Updates the user's full name, phone number, profile picture and preferred language.
        /// All fields are optional. Only supplied fields are updated.
        /// </remarks>
        /// <param name="request">The profile information to update.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Profile updated successfully. Returns the updated authentication response.</response>
        /// <response code="400">Validation failed. See fieldErrors in the response.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="404">The user account was not found.</response>
        /// <response code="429">Too many requests.</response>
        [HttpPut("profile")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> EditProfile(
            EditProfileRequest request,
            CancellationToken ct = default) =>
            (await sender.Send(
                new EditProfileCommand(
                    request.FullName,
                    request.PhoneNumber,
                    request.ProfilePictureUrl,
                    request.Language,
                    Client(request.DeviceId)),
                ct))
            .ToActionResult(
                this,
                message: "Profile Updated Successfully");


        /// <summary>Signs in with email and password.</summary>
        /// <remarks>
        /// Returns a short-lived access token (15 minutes) and a refresh token
        /// (30 days, sliding, 90 days maximum).
        /// Store both in secure storage. Five failed attempts lock the account for 15 minutes.
        /// An unknown email and a wrong password return the same error, so emails cannot be discovered.
        /// </remarks>
        /// <param name="request">Email, password and the client's device id.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Signed in. Returns ApiResponse containing tokens and profile.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Auth.InvalidCredentials or Auth.LockedOut.</response>
        /// <response code="429">Too many requests. Limit: 10 per minute per IP.</response>
        [HttpPost("login")]
        [AllowAnonymous, EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct=default) =>
            (await sender.Send(
                new LoginCommand(request.Email,request.Phone, request.Password, Client(request.DeviceId)), ct))
            .ToActionResult(this);

        /// <summary>Exchanges a refresh token for a new access token and a new refresh token.</summary>
        /// <remarks>
        /// Refresh tokens are single-use. Always replace both stored tokens with the new pair.
        /// A token that was just used can be retried for 10 seconds (lost response or parallel request).
        /// Presenting an already used token after that window ends ends the whole session and returns
        /// Auth.RefreshTokenReused; the client must then show the login screen.
        /// Only one refresh call should run at a time in the app.
        /// </remarks>
        /// <param name="request">The current refresh token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">New token pair in ApiResponse.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Auth.InvalidRefreshToken, Auth.RefreshTokenReused or Auth.LockedOut. Sign in again.</response>
        /// <response code="429">Too many requests. Limit: 10 per minute per IP.</response>
        [HttpPost("refresh")]
        [AllowAnonymous, EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct=default) =>
            (await sender.Send(
                new RefreshCommand(request.RefreshToken, Client(request.DeviceId)), ct))
            .ToActionResult(this);

        /// <summary>Signs out the current device.</summary>
        /// <remarks>
        /// Needs no access token on purpose, because it may already have expired; the refresh token is the proof.
        /// Always succeeds, even for an unknown token. The access token stays valid until it expires (at most 15 minutes).
        /// </remarks>
        /// <param name="request">The refresh token of the session to end.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Session ended. Returns ApiResponse.</response>
        /// <response code="400">Validation failed.</response>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct=default) =>
            (await sender.Send(new LogoutCommand(request.RefreshToken), ct))
            .ToActionResult(this);

        /// <summary>Signs out every device of the current user.</summary>
        /// <remarks>Requires a valid access token.</remarks>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">All sessions ended. Returns ApiResponse.</response>
        /// <response code="401">Missing or invalid access token.</response>
        [HttpPost("logout-all")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAll(CancellationToken ct=default) =>
            (await sender.Send(new LogoutAllCommand(), ct))
            .ToActionResult(this);

        /// <summary>Sends a password reset otp to the user's email Or Phone.</summary>
        /// <remarks>
        /// Always answers with the same success response, whether or not the (email or phone) is registered,
        /// so the endpoint cannot be used to discover accounts. The reset otp is valid for 5 minute.
        /// </remarks>
        /// <param name="request">The account's email.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Request accepted. Returns ApiResponse.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="429">Too many requests. Limit: 10 per minute per IP.</response>
        [HttpPost("forgot-password")]
        [AllowAnonymous, EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct=default) =>
            (await sender.Send(new ForgotPasswordCommand(request.Email, request.Phone), ct))
            .ToActionResult(this);

        /// <summary>Sets a new password using the token from the reset email Or Phone.</summary>
        /// <remarks>
        /// On success the account lockout is cleared and every device is signed out.
        /// </remarks>
        /// <param name="request">Email Or Phone, reset otp and the new password.</param>
        /// <response code="200">Password changed. Returns ApiResponse.</response>
        /// <response code="400">Validation failed, or the otp is invalid or expired (Auth.InvalidResetOtp).</response>
        /// <response code="429">Too many requests. Limit: 10 per minute per IP.</response>
        [HttpPost("reset-password")]
        [AllowAnonymous, EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct=default) =>
            (await sender.Send(
                new ResetPasswordCommand(request.Email,request.Phone, request.Otp, request.NewPassword), ct))
            .ToActionResult(this);

        /// <summary>Changes the password of the signed-in user.</summary>
        /// <remarks>
        /// Requires a valid access token. After a successful change every device, including this one,
        /// must sign in again.
        /// </remarks>
        /// <param name="request">The current and the new password.</param>
        /// <param name="ct"></param>
        /// <response code="200">Password changed. Returns ApiResponse.</response>
        /// <response code="400">Validation failed, or the current password is wrong (Auth.WrongCurrentPassword).</response>
        /// <response code="401">Missing or invalid access token.</response>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct=default) =>
            (await sender.Send(
                new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct))
            .ToActionResult(this);


        #region Confirmations

        /// <summary>
        /// Requests an email confirmation OTP for the authenticated user.
        /// </summary>
        /// <remarks>
        /// Requires a valid access token.
        /// The user's email is retrieved from the authenticated identity.
        /// An OTP is generated and sent to the user's registered email address.
        /// No request body is required.
        /// </remarks>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Email confirmation OTP request accepted.</response>
        /// <response code="400">Email is already confirmed or the request cannot be processed.</response>
        /// <response code="401">Missing or invalid access token.</response>
        /// <response code="429">Too many requests. Rate limit exceeded.</response>
        [HttpPost("request-email-confirmation")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RequestEmailConfirmation(
            CancellationToken ct = default) =>
            (await sender.Send(new RequestEmailConfirmationCommand(), ct))
                .ToActionResult(this);


        /// <summary>
        /// Verifies the authenticated user's email address using an OTP.
        /// </summary>
        /// <remarks>
        /// Requires a valid access token.
        /// The user's email is retrieved from the authenticated identity.
        /// The OTP must match the code previously sent to the user's email.
        /// </remarks>
        /// <param name="request">The received verification OTP.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Email successfully verified.</response>
        /// <response code="400">Invalid, expired, or previously used OTP.</response>
        /// <response code="401">Missing or invalid access token.</response>
        /// <response code="429">Too many requests. Rate limit exceeded.</response>
        [HttpPost("verify-email")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyEmail(
            ConfirmEmailRequest request,
            CancellationToken ct = default) =>
            (await sender.Send(new ConfirmEmailCommand(request.Otp), ct))
                .ToActionResult(this);


        /// <summary>
        /// Requests a phone confirmation OTP for the authenticated user.
        /// </summary>
        /// <remarks>
        /// Requires a valid access token.
        /// The user's phone number is retrieved from the authenticated identity.
        /// An OTP is generated and sent to the user's registered phone number.
        /// No request body is required.
        /// </remarks>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Phone confirmation OTP request accepted.</response>
        /// <response code="400">Phone is already confirmed or the request cannot be processed.</response>
        /// <response code="401">Missing or invalid access token.</response>
        /// <response code="429">Too many requests. Rate limit exceeded.</response>
        [HttpPost("request-phone-confirmation")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RequestPhoneConfirmation(
            CancellationToken ct = default) =>
            (await sender.Send(new RequestPhoneConfirmationCommand(), ct))
                .ToActionResult(this);


        /// <summary>
        /// Verifies the authenticated user's phone number using an OTP.
        /// </summary>
        /// <remarks>
        /// Requires a valid access token.
        /// The user's phone number is retrieved from the authenticated identity.
        /// The OTP must match the code previously sent to the user's phone.
        /// </remarks>
        /// <param name="request">The received verification OTP.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Phone successfully verified.</response>
        /// <response code="400">Invalid, expired, or previously used OTP.</response>
        /// <response code="401">Missing or invalid access token.</response>
        /// <response code="429">Too many requests. Rate limit exceeded.</response>
        [HttpPost("verify-phone")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyPhone(
            ConfirmPhoneRequest request,
            CancellationToken ct = default) =>
            (await sender.Send(new ConfirmPhoneCommand(request.Otp), ct))
                .ToActionResult(this);

        #endregion

        /// <summary>
        /// Collects where the request came from (device id, IP address, user agent) for session records.
        /// </summary>
        private ClientInfo Client(Guid? deviceId) => new(
            deviceId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }
}
