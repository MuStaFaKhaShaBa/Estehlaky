using System.Text;
using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Domain.Core.IdentitySchema;
using Estehlaky.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Estehlaky.UnitTests.Infrastructure;

public class TokenServiceTests
{
    private const string Key = "unit-test-signing-key-that-is-long-enough-123456";

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "estehlaky-tests",
        Audience = "estehlaky-tests-client",
        Key = Key,
        AccessTokenMinutes = 15
    };

    private readonly DateTime _now = DateTime.UtcNow;
    private readonly TokenService _service;
    private readonly AppUser _user = new() { Id = Guid.NewGuid(), Email = "user@test.local" };

    public TokenServiceTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(_now);
        _service = new TokenService(Microsoft.Extensions.Options.Options.Create(Settings), clock);
    }

    [Fact]
    public void Access_token_carries_subject_email_issuer_and_audience()
    {
        var (token, _) = _service.CreateAccessToken(_user);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal(_user.Id.ToString(), jwt.Subject);
        Assert.Equal("user@test.local", jwt.GetClaim("email").Value);
        Assert.Equal("estehlaky-tests", jwt.Issuer);
        Assert.Contains("estehlaky-tests-client", jwt.Audiences);
    }

    [Fact]
    public void Access_token_expires_after_the_configured_minutes()
    {
        var (token, expiresAt) = _service.CreateAccessToken(_user);

        Assert.Equal(_now.AddMinutes(15), expiresAt);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.True(Math.Abs((jwt.ValidTo - expiresAt).TotalSeconds) < 2);
    }

    [Fact]
    public async Task Access_token_is_valid_for_the_signing_key_and_invalid_for_another()
    {
        var (token, _) = _service.CreateAccessToken(_user);
        var handler = new JsonWebTokenHandler();

        TokenValidationParameters Parameters(string key) => new()
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true
        };

        Assert.True((await handler.ValidateTokenAsync(token, Parameters(Key))).IsValid);
        Assert.False((await handler.ValidateTokenAsync(token, Parameters("a-completely-different-signing-key-0123456789"))).IsValid);
    }

    [Fact]
    public void Every_access_token_has_its_own_jti()
    {
        var first = new JsonWebTokenHandler().ReadJsonWebToken(_service.CreateAccessToken(_user).Token);
        var second = new JsonWebTokenHandler().ReadJsonWebToken(_service.CreateAccessToken(_user).Token);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Refresh_tokens_are_unique_url_safe_and_long()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => TokenService.GenerateRefreshToken()).ToList();

        Assert.Equal(50, tokens.Distinct().Count());
        Assert.All(tokens, t =>
        {
            Assert.True(t.Length >= 86);   // 64 random bytes in base64url
            Assert.DoesNotContain('+', t);
            Assert.DoesNotContain('/', t);
            Assert.DoesNotContain('=', t);
        });
    }

    [Fact]
    public void Hash_is_deterministic_hex_and_never_the_raw_token()
    {
        var raw = TokenService.GenerateRefreshToken();

        var hash = TokenService.Hash(raw);

        Assert.Equal(hash, TokenService.Hash(raw));
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]{64}$", hash);
        Assert.NotEqual(raw, hash);
        Assert.NotEqual(hash, TokenService.Hash(raw + "x"));
    }
}
