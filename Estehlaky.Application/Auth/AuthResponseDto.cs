using Estehlaky.Application.Me;

namespace Estehlaky.Application.Auth;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
