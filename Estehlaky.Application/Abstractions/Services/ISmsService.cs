namespace Estehlaky.Application.Abstractions.Services;

/// <summary>
/// Defines the contract for sending transactional SMS messages.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a one-time verification code to the specified phone number.
    /// </summary>
    /// <param name="toPhoneNumber">The recipient's phone number.</param>
    /// <param name="otp">The one-time verification code.</param>
    /// <param name="language">The language used to localize the SMS content.</param>
    /// <param name="ct">A token used to cancel the asynchronous operation.</param>
    Task SendOtpAsync(
        string toPhoneNumber,
        string otp,
        string language,
        CancellationToken ct);
}