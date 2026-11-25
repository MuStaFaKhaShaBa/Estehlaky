using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;
using Estehlaky.Domain.Core.IdentitySchema;
using Estehlaky.Domain.Enums;
using Estehlaky.Infrastructure.Data;
using Estehlaky.Infrastructure.Sms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using System.Data;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace Estehlaky.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<AppUser> users,
    AppDbContext db,
    TokenService tokens,
    IEmailService email,
    ISmsService smsService,
    IOtpService otpService,
    IClock clock,
    IOptions<JwtOptions> jwtOptions,
    IOptions<SentSmsOptions> sentSmsOptions,
    IOptions<OtpOptions> otpOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly SentSmsOptions _sentSms = sentSmsOptions.Value;
    private readonly OtpOptions _otp = otpOptions.Value;

    // Pattern used by every public method below:
    //   * expected outcomes (wrong password, expired token, wrong code, ...) are returned as Result errors;
    //   * unexpected exceptions are caught here, logged with the operation name, and turned into a
    //     meaningful Error by ToError(). Exception text is never sent to the client.
    //   * OperationCanceledException is never caught: a cancelled request is not an error.
    //
    // One-time codes (OTPs) live in one table (ConfirmationOtps) and every query filters by Purpose,
    // so a code issued for one purpose can never be used for another.

    // ------------------------------------------------------------------ register
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        string emailAddress, string password, string? fullName, string? phone, string language,
        ClientInfo client, CancellationToken ct)
    {
        try
        {
            emailAddress = emailAddress.Trim();

            var user = new AppUser
            {
                Id = Guid.CreateVersion7(),
                UserName = emailAddress,
                Email = emailAddress,
                PhoneNumber = phone?.Trim(),
                FullName = fullName?.Trim(),
                PreferredLanguage = language,
                CreatedAt = clock.UtcNow
            };

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
                return Result.Failure<AuthResponseDto>(MapIdentityErrors(created));

            try
            {
                await email.SendWelcomeEmailAsync(emailAddress, user.FullName ?? "", language, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send welcome email to {Email} in {Operation}", emailAddress, nameof(RegisterAsync));
            }

            // If this step fails the account already exists; the user can simply sign in.
            return await StartSessionAsync(user, client, ct);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            // Two sign-ups with the same email at the same moment: the database unique index decides.
            logger.LogWarning(ex, "Duplicate email rejected by the database in {Operation}", nameof(RegisterAsync));
            return AuthErrors.EmailTaken;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(RegisterAsync));
        }
    }

    // ------------------------------------------------------------------ Edit Profile

    public async Task<Result<UserDto>> EditProfileAsync(
        Guid userId,
        string? fullName,
        string? phoneNumber,
        string? profilePictureUrl,
        string? language,
        ClientInfo client,
        CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;

            if (fullName is not null)
                user.FullName = fullName.Trim();

            if (phoneNumber is not null)
                user.PhoneNumber = phoneNumber.Trim();

            if (profilePictureUrl is not null)
                user.ProfilePictureUrl = profilePictureUrl.Trim();

            if (language is not null)
                user.PreferredLanguage = language;

            var result = await users.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return Result.Failure<UserDto>(MapIdentityErrors(result));
            }

            // Generate the updated user profile.
            return Result.Success(ToDto(user));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(EditProfileAsync));
        }
    }


    // ------------------------------------------------------------------ login
    public async Task<Result<AuthResponseDto>> LoginAsync(
        string emailAddress, string? phone, string password, ClientInfo client, CancellationToken ct)
    {
        try
        {
            var user = !string.IsNullOrWhiteSpace(emailAddress) ?
                await users.FindByEmailAsync(emailAddress.Trim())
                : await FindByPhoneAsync(phone!, ct);

            // Same generic answer for "no such user" and "wrong password" (no user enumeration).
            if (user is null || user.DeletedAt is not null)
                return AuthErrors.InvalidCredentials;

            if (await users.IsLockedOutAsync(user))
                return AuthErrors.LockedOut;

            if (!await users.CheckPasswordAsync(user, password))
            {
                await users.AccessFailedAsync(user);
                return await users.IsLockedOutAsync(user)
                    ? AuthErrors.LockedOut
                    : AuthErrors.InvalidCredentials;
            }

            await users.ResetAccessFailedCountAsync(user);
            return await StartSessionAsync(user, client, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(LoginAsync));
        }
    }

    // ------------------------------------------------------------------ refresh (rotation + reuse detection)
    public async Task<Result<AuthResponseDto>> RefreshAsync(
        string refreshToken, ClientInfo client, CancellationToken ct)
    {
        try
        {
            var now = clock.UtcNow;
            var hash = TokenService.Hash(refreshToken);

            var token = await db.RefreshTokens.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
                return AuthErrors.InvalidRefreshToken;

            var user = await users.FindByIdAsync(token.UserId.ToString());
            if (user is null || user.DeletedAt is not null)
            {
                await RevokeFamilyAsync(token.FamilyId, "UserUnavailable", now, ct);
                return AuthErrors.InvalidRefreshToken;
            }

            if (await users.IsLockedOutAsync(user))
                return AuthErrors.LockedOut;

            var next = NewRefreshToken(user.Id, token.FamilyId, token.FamilyStartedAt, client, now, out var rawNext);

            var usedAt = token.UsedAt;
            if (usedAt is null)
            {
                // Claim the token with one atomic UPDATE. If two requests race, only one gets claimed == 1.
                var claimed = await db.RefreshTokens
                    .Where(t => t.Id == token.Id && t.UsedAt == null && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.UsedAt, (DateTime?)now)
                        .SetProperty(t => t.ReplacedByTokenId, (Guid?)next.Id), ct);

                if (claimed == 0)
                {
                    var fresh = await db.RefreshTokens.AsNoTracking().FirstAsync(t => t.Id == token.Id, ct);
                    if (fresh.RevokedAt is not null) return AuthErrors.InvalidRefreshToken;
                    usedAt = fresh.UsedAt;   // another request used it a moment ago
                }
            }

            if (usedAt is not null && now - usedAt.Value > TimeSpan.FromSeconds(_jwt.ReuseGraceSeconds))
            {
                // Already used and not a quick retry: assume the token was stolen. Kill the whole session.
                await RevokeFamilyAsync(token.FamilyId, "ReuseDetected", now, ct);
                return AuthErrors.RefreshTokenReused;
            }

            // First use, or a retry inside the grace window: issue a fresh token in the same family.
            // If saving fails after the claim above, a retry within the grace window still succeeds.
            db.RefreshTokens.Add(next);
            await db.SaveChangesAsync(ct);

            return BuildResponse(user, rawNext, next.ExpiresAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(RefreshAsync));
        }
    }

    // ------------------------------------------------------------------ logout
    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            var hash = TokenService.Hash(refreshToken);
            var token = await db.RefreshTokens.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            // Unknown token: nothing to do, still a success (logout is idempotent).
            if (token is not null)
                await RevokeFamilyAsync(token.FamilyId, "Logout", clock.UtcNow, ct);

            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The app keeps its tokens and can retry the logout.
            return ToError(ex, nameof(LogoutAsync));
        }
    }

    public async Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            await RevokeAllAsync(userId, "LogoutAll", ct);
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(LogoutAllAsync));
        }
    }

    // ------------------------------------------------------------------ forgot / reset password

    public async Task<Result> ForgotPasswordAsync(
        string emailAddress,
        string? phone,
        CancellationToken ct)
    {
        // Step 1: find the account. A failure here happens whether or not the account exists,
        // so it is safe to report it to the caller.
        AppUser? user;
        try
        {
            user = string.IsNullOrEmpty(phone)
                ? await users.FindByEmailAsync(emailAddress.Trim())
                : await FindByPhoneAsync(phone, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(ForgotPasswordAsync));
        }

        // Do not reveal whether an account exists.
        if (user is null || user.DeletedAt is not null)
            return Result.Success();

        // Step 2: issue and send the code. From here on the account is known to exist, so any failure
        // is logged and the caller still gets the same success answer.
        return await IssueAndSendSafelyAsync(
            user,
            PurposeOfOTP.PasswordReset,
            sendEmail: !string.IsNullOrEmpty(emailAddress),
            sendSms: !string.IsNullOrEmpty(user.PhoneNumber),
            nameof(ForgotPasswordAsync),
            ct);
    }

    public async Task<Result> ResetPasswordAsync(
         string? emailAddress,
         string? phone,
         string otp,
         string newPassword,
         CancellationToken ct)
    {
        try
        {
            // Find the account using email or phone.
            var user = !string.IsNullOrWhiteSpace(emailAddress)
                ? await users.FindByEmailAsync(emailAddress.Trim())
                : !string.IsNullOrWhiteSpace(phone)
                    ? await FindByPhoneAsync(phone, ct)
                    : null;

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.InvalidResetToken;

            var now = clock.UtcNow;

            var otpRecord = await FindActiveOtpAsync(user.Id, PurposeOfOTP.PasswordReset, now, ct);
            if (otpRecord is null)
                return AuthErrors.InvalidResetOtp;

            // Verify the submitted code.
            if (!otpService.Verify(user.Id, otp, otpRecord.CodeHash))
            {
                await RegisterFailedAttemptAsync(otpRecord, now, ct);
                return AuthErrors.InvalidResetOtp;
            }

            // If anything below throws or returns early, the transaction is disposed without commit
            // and rolls back, so the code is not burned by a failed reset.
            await using var transaction =
                await db.Database.BeginTransactionAsync(ct);

            // Atomically consume the code.
            if (await ConsumeOtpAsync(otpRecord, now, ct) == 0)
                return AuthErrors.InvalidResetOtp;

            var identityToken =
                await users.GeneratePasswordResetTokenAsync(user);

            var reset = await users.ResetPasswordAsync(
                user,
                identityToken,
                newPassword);

            if (!reset.Succeeded)
                return Result.Failure(MapIdentityErrors(reset));

            await users.SetLockoutEndDateAsync(user, null);
            await users.ResetAccessFailedCountAsync(user);

            await RevokeAllAsync(user.Id, "PasswordReset", ct);

            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(ResetPasswordAsync));
        }
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());
            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;

            var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changed.Succeeded)
                return Result.Failure(MapIdentityErrors(changed));

            // Every device must sign in again with the new password.
            await RevokeAllAsync(userId, "PasswordChanged", ct);
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(ChangePasswordAsync));
        }
    }

    // ------------------------------------------------------------------ confirm email / phone

    #region Confrimation Email And Phone

    public async Task<Result> RequestEmailConfirmationAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;
            if (user.EmailConfirmed)
                return AuthErrors.EmailAlreadyConfirmed;

            // Issue and send the code. From here on the account is known to exist, so any failure
            // is logged and the caller still gets the same success answer.

            return await IssueAndSendSafelyAsync(
                user,
                PurposeOfOTP.ConfirmationEmail,
                sendEmail: true,
                sendSms: false,
                nameof(RequestEmailConfirmationAsync),
                ct);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(RequestEmailConfirmationAsync));
        }
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string otp, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;
            if (user.EmailConfirmed)
                return AuthErrors.EmailAlreadyConfirmed;

            return await ConfirmAsync(
               user,
               PurposeOfOTP.ConfirmationEmail,
               otp,
               markConfirmed: u =>
               {
                   u.EmailConfirmed = true;
                   return users.UpdateAsync(u);
               },
               ct);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(ConfirmEmailAsync));
        }
    }

    public async Task<Result> RequestPhoneConfirmationAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;
            if (user.PhoneNumberConfirmed)
                return AuthErrors.PhoneAlreadyConfirmed;

            // Issue and send the code. From here on the account is known to exist, so any failure
            // is logged and the caller still gets the same success answer.

            return await IssueAndSendSafelyAsync(
                user,
                PurposeOfOTP.ConfirmationPhone,
                sendEmail: false,
                sendSms: true,
                nameof(RequestPhoneConfirmationAsync),
                ct);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(RequestPhoneConfirmationAsync));
        }
    }

    public async Task<Result> ConfirmPhoneAsync(Guid userId, string otp, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());

            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;
            if (user.PhoneNumberConfirmed)
                return AuthErrors.PhoneAlreadyConfirmed;

            return await ConfirmAsync(
               user,
               PurposeOfOTP.ConfirmationPhone,
               otp,
               markConfirmed: u =>
               {
                   u.PhoneNumberConfirmed = true;
                   return users.UpdateAsync(u);
               },
               ct);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(ConfirmPhoneAsync));
        }
    }



    #endregion

    // ------------------------------------------------------------------ me
    public async Task<Result<UserDto>> GetUserAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var user = await users.FindByIdAsync(userId.ToString());
            if (user is null || user.DeletedAt is not null)
                return AuthErrors.UserNotFound;

            return ToDto(user);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ToError(ex, nameof(GetUserAsync));
        }
    }

    // ------------------------------------------------------------------ OTP helpers (all purpose-aware)

    /// <summary>The newest code of this purpose that is unused, not expired and still has attempts left.</summary>
    private Task<ConfirmationOtps?> FindActiveOtpAsync(
        Guid userId, PurposeOfOTP purpose, DateTime now, CancellationToken ct) =>
        db.ConfirmationOtps
            .Where(x =>
                x.UserId == userId &&
                x.Purpose == purpose &&
                x.UsedAt == null &&
                x.ExpiresAt > now &&
                x.FailedAttempts < _otp.MaxAttempts)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>Counts a wrong guess. Includes the purpose, so a code of another purpose is never touched.</summary>
    private Task<int> RegisterFailedAttemptAsync(ConfirmationOtps record, DateTime now, CancellationToken ct) =>
        db.ConfirmationOtps
            .Where(x =>
                x.Id == record.Id &&
                x.Purpose == record.Purpose &&
                x.UsedAt == null &&
                x.ExpiresAt > now &&
                x.FailedAttempts < _otp.MaxAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.FailedAttempts, x => x.FailedAttempts + 1), ct);

    /// <summary>Marks the code as used with one atomic UPDATE. Returns 0 if someone else used it first.</summary>
    private Task<int> ConsumeOtpAsync(ConfirmationOtps record, DateTime now, CancellationToken ct) =>
        db.ConfirmationOtps
            .Where(x =>
                x.Id == record.Id &&
                x.UserId == record.UserId &&
                x.Purpose == record.Purpose &&
                x.CodeHash == record.CodeHash &&
                x.UsedAt == null &&
                x.ExpiresAt > now &&
                x.FailedAttempts < _otp.MaxAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UsedAt, (DateTime?)now), ct);

    /// <summary>
    /// Creates a new code of this purpose and invalidates the older unused ones of the same purpose.
    /// Returns null when the resend cooldown has not passed yet.
    /// </summary>
    private async Task<string?> CreateOtpAsync(
        Guid userId, PurposeOfOTP purpose, DateTime now, CancellationToken ct)
    {
        // Serialize creation to avoid concurrent resend requests.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var latest = await db.ConfirmationOtps
            .Where(x => x.UserId == userId && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Enforce resend cooldown (per purpose).
        if (latest is not null &&
            now - latest.CreatedAt < TimeSpan.FromSeconds(_otp.ResendCooldownSeconds))
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        // Invalidate previous unused codes of THIS purpose only.
        await db.ConfirmationOtps
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.UsedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UsedAt, (DateTime?)now), ct);

        var otp = otpService.Generate();

        db.ConfirmationOtps.Add(new ConfirmationOtps
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CodeHash = otpService.Hash(userId, otp),
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_otp.ExpirationMinutes),
            FailedAttempts = 0
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return otp;
    }

    /// <summary>Creates a code and delivers it. Each channel is isolated: if the email fails, the SMS is still tried.</summary>
    private async Task IssueAndSendAsync(
     AppUser user,
     PurposeOfOTP purpose,
     bool sendEmail,
     bool sendSms,
     CancellationToken ct)
    {
        var code = await CreateOtpAsync(user.Id, purpose, clock.UtcNow, ct);
        if (code is null)
            return; // Resend cooldown: silently do nothing

        var language = user.PreferredLanguage;

        if (sendEmail && !string.IsNullOrWhiteSpace(user.Email))
        {
            var to = user.Email;

            await TrySendAsync("email", user.Id, purpose, async () =>
            {
                await email.SendOtpAsync(to, code, language, ct);
            });
        }

        if (sendSms && !string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            var to = user.PhoneNumber;

            await TrySendAsync("sms", user.Id, purpose, async () =>
            {
                await smsService.SendOtpAsync(to, code, language, ct);
            });
        }
    }

    /// <summary>
    /// For endpoints that must not reveal whether an account exists: any failure after the account was
    /// found is logged and the caller still gets the same success answer.
    /// </summary>
    private async Task<Result> IssueAndSendSafelyAsync(
        AppUser user, PurposeOfOTP purpose, bool sendEmail, bool sendSms, string operation, CancellationToken ct)
    {
        try
        {
            await IssueAndSendAsync(user, purpose, sendEmail, sendSms, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "{Operation} failed after the account was found. UserId {UserId}, Purpose {Purpose}",
                operation, user.Id, purpose);
        }

        return Result.Success();
    }

    /// <summary>
    /// Shared confirmation flow: find the active code of this purpose, verify it, then consume it and mark
    /// the email/phone as confirmed in one transaction. Unknown account, no active code and wrong code all
    /// give the same error, so the endpoint reveals nothing about which accounts exist.
    /// </summary>
    private async Task<Result> ConfirmAsync(
        AppUser? user,
        PurposeOfOTP purpose,
        string otp,
        Func<AppUser, Task<IdentityResult>> markConfirmed,
        CancellationToken ct)
    {
        if (user is null || user.DeletedAt is not null)
            return AuthErrors.InvalidConfirmationOtp;

        var now = clock.UtcNow;

        var record = await FindActiveOtpAsync(user.Id, purpose, now, ct);
        if (record is null)
            return AuthErrors.InvalidConfirmationOtp;

        if (!otpService.Verify(user.Id, otp, record.CodeHash))
        {
            await RegisterFailedAttemptAsync(record, now, ct);
            return AuthErrors.InvalidConfirmationOtp;
        }

        // Consuming the code and confirming the account succeed or fail together.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (await ConsumeOtpAsync(record, now, ct) == 0)
            return AuthErrors.InvalidConfirmationOtp;

        var confirmed = await markConfirmed(user);
        if (!confirmed.Succeeded)
            return Result.Failure(MapIdentityErrors(confirmed));

        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Sends one notification. A failed channel is logged (without the code or the address) and never throws.
    /// </summary>
    private async Task TrySendAsync(string channel, Guid userId, PurposeOfOTP purpose, Func<Task> send)
    {
        try
        {
            send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Failed to send the {Purpose} code by {Channel} to user {UserId}",
                purpose, channel, userId);
        }
    }

    private Task<AppUser?> FindByPhoneAsync(string phone, CancellationToken ct)
    {
        var normalized = phone.Trim();
        return users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalized, cancellationToken: ct);
    }

    // ------------------------------------------------------------------ session helpers
    private async Task<Result<AuthResponseDto>> StartSessionAsync(AppUser user, ClientInfo client, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var token = NewRefreshToken(user.Id, Guid.CreateVersion7(), now, client, now, out var raw);

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);

        // Housekeeping only: a failure here must not fail a login that already succeeded.
        try
        {
            await EnforceSessionLimitAsync(user.Id, now, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not enforce the session limit for user {UserId}", user.Id);
        }

        return BuildResponse(user, raw, token.ExpiresAt);
    }

    private RefreshToken NewRefreshToken(
        Guid userId, Guid familyId, DateTime familyStartedAt,
        ClientInfo client, DateTime now, out string rawToken)
    {
        rawToken = TokenService.GenerateRefreshToken();

        var slidingExpiry = now.AddDays(_jwt.RefreshTokenDays);
        var absoluteExpiry = familyStartedAt.AddDays(_jwt.RefreshTokenAbsoluteDays);

        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            DeviceId = client.DeviceId,
            TokenHash = TokenService.Hash(rawToken),
            FamilyId = familyId,
            FamilyStartedAt = familyStartedAt,
            CreatedAt = now,
            ExpiresAt = slidingExpiry < absoluteExpiry ? slidingExpiry : absoluteExpiry,
            CreatedByIp = Truncate(client.IpAddress, 45),
            UserAgent = Truncate(client.UserAgent, 256)
        };
    }

    private AuthResponseDto BuildResponse(AppUser user, string rawRefreshToken, DateTime refreshExpiresAt)
    {
        var (access, accessExpires) = tokens.CreateAccessToken(user);
        return new AuthResponseDto(access, accessExpires, rawRefreshToken, refreshExpiresAt, ToDto(user));
    }

    private static UserDto ToDto(AppUser u) =>
        new(u.Id, u.Email!, u.PhoneNumber, u.FullName, u.PreferredLanguage, u.Theme);

    private Task<int> RevokeFamilyAsync(Guid familyId, string reason, DateTime now, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, (DateTime?)now)
                .SetProperty(t => t.RevokedReason, reason), ct);

    private Task<int> RevokeAllAsync(Guid userId, string reason, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, (DateTime?)now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    /// <summary>Keeps only the newest N sessions per user; older ones are signed out.</summary>
    private async Task EnforceSessionLimitAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var families = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .GroupBy(t => t.FamilyId)
            .Select(g => new { FamilyId = g.Key, Started = g.Min(t => t.FamilyStartedAt) })
            .OrderByDescending(x => x.Started)
            .ToListAsync(ct);

        var excess = families.Skip(_jwt.MaxActiveSessions).Select(x => x.FamilyId).ToList();
        if (excess.Count == 0) return;

        await db.RefreshTokens
            .Where(t => excess.Contains(t.FamilyId) && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, (DateTime?)now)
                .SetProperty(t => t.RevokedReason, "SessionLimit"), ct);
    }

    // ------------------------------------------------------------------ exception mapping

    /// <summary>
    /// Logs an unexpected exception with the operation name and converts it into an Error
    /// the client can act on. The exception text never leaves the server.
    /// </summary>
    private Error ToError(Exception ex, string operation)
    {
        var sql = GetSqlException(ex);

        // Two requests changed the same row at the same time.
        if (ex is DbUpdateConcurrencyException)
        {
            logger.LogWarning(ex, "Concurrency conflict in {Operation}", operation);
            return AuthErrors.ConcurrentRequest;
        }

        // SQL Server picked this request as the deadlock victim (1205): retrying normally works.
        if (sql is { Number: 1205 })
        {
            logger.LogWarning(ex, "Deadlock in {Operation}", operation);
            return AuthErrors.ConcurrentRequest;
        }

        // Unique index violation (2601 / 2627).
        if (sql is { Number: 2601 or 2627 })
        {
            logger.LogWarning(ex, "Duplicate value rejected by the database in {Operation}", operation);
            return AuthErrors.DuplicateRecord;
        }

        // Timeout (-2), dropped connection, database failover and other transient SQL problems.
        if (sql is { IsTransient: true } or { Number: -2 } || ex is TimeoutException)
        {
            logger.LogError(ex, "Database unavailable in {Operation}", operation);
            return AuthErrors.ServiceUnavailable;
        }

        // Any other failure while saving (constraint, truncation, ...).
        if (ex is DbUpdateException)
        {
            logger.LogError(ex, "Database update failed in {Operation}", operation);
            return AuthErrors.DatabaseError;
        }

        logger.LogError(ex, "Unexpected error in {Operation}", operation);
        return AuthErrors.Unexpected;
    }

    private static SqlException? GetSqlException(Exception ex) =>
        ex as SqlException ?? ex.InnerException as SqlException;

    private static bool IsUniqueViolation(Exception ex) =>
        GetSqlException(ex) is { Number: 2601 or 2627 };

    private static List<Error> MapIdentityErrors(IdentityResult result)
    {
        var errors = new List<Error>();
        var fields = new List<FieldError>();

        foreach (var e in result.Errors)
        {
            switch (e.Code)
            {
                case "DuplicateEmail":
                case "DuplicateUserName":
                    errors.Add(AuthErrors.EmailTaken);
                    break;
                case "InvalidToken":
                    errors.Add(AuthErrors.InvalidResetToken);
                    break;
                case "PasswordMismatch":
                    errors.Add(AuthErrors.WrongCurrentPassword);
                    break;
                case "ConcurrencyFailure":
                    errors.Add(AuthErrors.ConcurrentRequest);
                    break;
                case var c when c.StartsWith("Password", StringComparison.Ordinal):
                    fields.Add(new FieldError("password", c, e.Description));
                    break;
                case var c when c.StartsWith("Invalid", StringComparison.Ordinal):
                    fields.Add(new FieldError("email", c, e.Description));
                    break;
                default:
                    errors.Add(Error.Failure($"Auth.{e.Code}", e.Description));
                    break;
            }
        }

        if (fields.Count > 0) errors.Add(Error.Validation(fields));
        return [.. errors.DistinctBy(e => e.Code)];
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];


}