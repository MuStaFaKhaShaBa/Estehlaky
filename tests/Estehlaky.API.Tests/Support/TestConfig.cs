namespace Estehlaky.API.Tests.Support;

/// <summary>Values the test host runs with. Fake on purpose: nothing here is a real secret.</summary>
public static class TestConfig
{
    public const string JwtKey = "integration-tests-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz";
    public const string JwtIssuer = "estehlaky-tests";
    public const string JwtAudience = "estehlaky-tests-client";

    /// <summary>Passes the Identity policy (10+ chars, upper, lower, digit, symbol, 4 unique).</summary>
    public const string Password = "Str0ng!Passw0rd";
    public const string OtherPassword = "An0ther!Passw0rd";

    /// <summary>A valid Base64 key of 48 bytes (the app requires at least 32). Fake: used only by the test host.</summary>
    public static readonly string OtpHashingKey =
        Convert.ToBase64String(Enumerable.Range(1, 48).Select(i => (byte)i).ToArray());

    /// <summary>
    /// Options the app validates at startup (ValidateOnStart). The real email sender is replaced in the test host,
    /// so these values only have to pass validation; nothing is ever sent through Resend.
    /// Several spellings of the sender property are listed: keys that match no property are ignored.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> StartupSettings = new Dictionary<string, string>
    {
        ["EmailServices:Resend:ApiKey"] = "re_test_key_never_used",
        ["EmailServices:Resend:SenderEmail"] = "noreply@test.local",
        ["EmailServices:Resend:FromEmail"] = "noreply@test.local",
        ["EmailServices:Resend:From"] = "noreply@test.local",
        ["EmailServices:Resend:Sender"] = "noreply@test.local",
        ["EmailServices:Resend:SenderAddress"] = "noreply@test.local",
        ["EmailServices:Resend:FromAddress"] = "noreply@test.local",
        ["PasswordResetOtp:HashingKey"] = OtpHashingKey
    };

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";
}
