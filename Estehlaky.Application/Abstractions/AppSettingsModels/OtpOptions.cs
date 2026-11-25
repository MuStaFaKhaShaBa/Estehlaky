namespace Estehlaky.Application.Abstractions.AppSettingsModels
{
    /// <summary>
    /// Defines the configuration settings for password reset OTP operations.
    /// </summary>
    public sealed class OtpOptions
    {
        public const string SectionName = "PasswordResetOtp";

        /// <summary>
        /// Gets the number of digits in a generated OTP.
        /// </summary>
        public int Length { get; init; } = 6;

        /// <summary>
        /// Gets the OTP validity duration in minutes.
        /// </summary>
        public int ExpirationMinutes { get; init; } = 5;

        /// <summary>
        /// Gets the maximum number of incorrect verification attempts.
        /// </summary>
        public int MaxAttempts { get; init; } = 5;

        /// <summary>
        /// Gets the minimum interval between OTP requests, in seconds.
        /// </summary>
        public int ResendCooldownSeconds { get; init; } = 60;

        /// <summary>
        /// Gets the secret used to generate keyed OTP hashes.
        /// </summary>
        public string HashingKey { get; init; } = string.Empty;
    }
}
