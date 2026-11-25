namespace Estehlaky.API.Contracts;

public sealed record RegisterRequest(
    string Email, string Password, string? Phone, string? FullName, string? Language, Guid? DeviceId);
public sealed record EditProfileRequest(
    string? FullName,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string? Language,
    Guid? DeviceId);

public sealed record LoginRequest(string Email, string? Phone, string Password, Guid? DeviceId);

public sealed record RefreshRequest(string RefreshToken, Guid? DeviceId);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email, string? Phone);

public sealed record ResetPasswordRequest(string Email, string? Phone, string Otp, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ConfirmEmailRequest(string Otp);
public sealed record ConfirmPhoneRequest(string Otp);
