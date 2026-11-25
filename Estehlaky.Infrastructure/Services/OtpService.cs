
using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Estehlaky.Infrastructure.Auth;

/// <summary>
/// Provides secure generation, hashing, and verification of one-time passwords (OTPs).
/// </summary>
public sealed class OtpService(
    IOptions<OtpOptions> options)
    : IOtpService
{
    private readonly OtpOptions _options = options.Value;

    /// <inheritdoc />
    public string Generate()
    {
        var max = (int)Math.Pow(10, _options.Length);

        return RandomNumberGenerator
            .GetInt32(0, max)
            .ToString($"D{_options.Length}", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public string Hash(Guid userId, string otp)
    {
        var key = Convert.FromBase64String(_options.HashingKey);

        var payload = $"password-reset:{userId:N}:{otp}";

        var hash = HMACSHA256.HashData(
            key,
            Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash);
    }

    /// <inheritdoc />
    public bool Verify(
        Guid userId,
        string otp,
        string expectedHash)
    {
        if (string.IsNullOrEmpty(otp) ||
            otp.Length != _options.Length ||
            !otp.All(c => c is >= '0' and <= '9'))
        {
            return false;
        }

        byte[] expected;

        try
        {
            expected = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expected.Length != 32)
            return false;

        var actual = Convert.FromHexString(Hash(userId, otp));

        return CryptographicOperations.FixedTimeEquals(
            actual,
            expected);
    }
}
