using Estehlaky.Domain.Enums;

namespace Estehlaky.Application.Me;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? Phone,
    string? FullName,
    string PreferredLanguage,
    ThemeMode Theme);
