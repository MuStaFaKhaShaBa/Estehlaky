namespace Estehlaky.Application.Abstractions.Services;

/// <summary>
/// Defines the contract for sending transactional emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a one-time verification code to the specified email address.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="otp">The one-time verification code.</param>
    /// <param name="language">The language used to localize the email content.</param>
    /// <param name="ct">A token used to cancel the asynchronous operation.</param>
    Task SendOtpAsync(
        string toEmail,
        string otp,
        string language,
        CancellationToken ct);

    /// <summary>
    /// Sends a welcome email to the specified email address.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="fullName">The recipient's full name.</param>
    /// <param name="language">The language used to localize the email content.</param>
    /// <param name="ct">A token used to cancel the asynchronous operation.</param>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string language,
        CancellationToken ct);
}