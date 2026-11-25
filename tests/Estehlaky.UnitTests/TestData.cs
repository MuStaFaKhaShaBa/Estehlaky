using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Me;
using Estehlaky.Domain.Enums;

namespace Estehlaky.UnitTests;

internal static class TestData
{
    public static readonly ClientInfo Client = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "127.0.0.1", "unit-tests");

    public static UserDto User(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "user@test.local", "Test User","1234567890", "en", ThemeMode.System);

    public static AuthResponseDto AuthResponse(Guid? userId = null) =>
        new("access", DateTime.UtcNow.AddMinutes(15), "refresh", DateTime.UtcNow.AddDays(30), User(userId));
}
