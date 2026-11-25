using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Estehlaky.API.Tests.Support;

/// <summary>Builds access tokens by hand, so tests can forge good and bad ones.</summary>
internal static class TestTokens
{
    public static string Create(
        Guid userId,
        string key = TestConfig.JwtKey,
        string issuer = TestConfig.JwtIssuer,
        string audience = TestConfig.JwtAudience,
        TimeSpan? lifetime = null,
        string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var now = DateTime.UtcNow;
        var life = lifetime ?? TimeSpan.FromMinutes(10);

        // A negative lifetime produces a token that expired that long ago.
        var expires = now.Add(life);
        var notBefore = life < TimeSpan.Zero ? expires.AddMinutes(-30) : now;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()), new Claim("email", "forged@test.local") }),
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
