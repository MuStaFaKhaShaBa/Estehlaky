using System.ComponentModel.DataAnnotations;

namespace Estehlaky.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = default!;
    [Required] public string Audience { get; set; } = default!;

    /// <summary>Signing key. At least 32 characters. Keep it in user-secrets / environment, never in appsettings.json.</summary>
    [Required, MinLength(32)] public string Key { get; set; } = default!;

    [Range(1, 120)] public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Each refresh token lives this long (sliding: renewed on every rotation).</summary>
    [Range(1, 365)] public int RefreshTokenDays { get; set; } = 30;

    /// <summary>Hard limit for a whole session family, so a stolen chain cannot live forever.</summary>
    [Range(1, 730)] public int RefreshTokenAbsoluteDays { get; set; } = 90;

    /// <summary>A just-used refresh token can be retried within this window (parallel or retried requests).</summary>
    [Range(0, 60)] public int ReuseGraceSeconds { get; set; } = 10;

    [Range(1, 50)] public int MaxActiveSessions { get; set; } = 10;

    [Range(0, 300)] public int ClockSkewSeconds { get; set; } = 30;
}
