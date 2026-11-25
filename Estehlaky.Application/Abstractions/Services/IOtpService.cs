
namespace Estehlaky.Application.Abstractions;

/// <summary>
/// Defines operations for generating and verifying one-time passwords (OTPs).
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a cryptographically secure numeric OTP.
    /// </summary>
    string Generate();

    /// <summary>
    /// Creates a secure keyed hash for a user's OTP.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="otp">The generated OTP.</param>
    string Hash(Guid userId, string otp);

    /// <summary>
    /// Verifies an OTP against its stored hash.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="otp">The OTP submitted by the user.</param>
    /// <param name="expectedHash">The previously stored OTP hash.</param>
    bool Verify(Guid userId, string otp, string expectedHash);
}
