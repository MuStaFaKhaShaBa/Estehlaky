namespace Estehlaky.Infrastructure.Seed.Models;

public class SeedUser
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Passwords are never in the JSON file. This key points to the secret: Seed:Passwords:{PasswordKey}
    /// (user-secrets in development, environment variable Seed__Passwords__{PasswordKey} in production).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    /// <summary>When true the user is created only in the Development environment.</summary>
    public bool DevelopmentOnly { get; set; }
}
