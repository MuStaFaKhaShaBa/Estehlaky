using System.Reflection;
using Estehlaky.Application.Security;
using Estehlaky.Infrastructure.Seed;
using Estehlaky.Infrastructure.Seed.Models;

namespace Estehlaky.UnitTests.Infrastructure;

/// <summary>
/// The seeder ignores failures silently (the result is not checked at startup), so a bad seed file
/// would simply leave the database without users. These tests catch that before deployment.
/// </summary>
public class SeedDataTests
{
    private static readonly List<SeedRole> RolesFile = SeedFileReader.Read<SeedRole>("Seed.Data.roles.json");
    private static readonly List<SeedUser> UsersFile = SeedFileReader.Read<SeedUser>("Seed.Data.users.json");

    [Fact]
    public void Every_role_constant_in_the_application_is_seeded()
    {
        var constants = typeof(Roles)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        var seeded = RolesFile.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(constants, role => Assert.Contains(role, seeded));
    }

    [Fact]
    public void Role_names_are_unique()
    {
        Assert.Equal(RolesFile.Count, RolesFile.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Seed_user_emails_are_unique()
    {
        Assert.Equal(UsersFile.Count, UsersFile.Select(u => u.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_role_given_to_a_seed_user_exists()
    {
        var seeded = RolesFile.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(UsersFile.SelectMany(u => u.Roles), role => Assert.Contains(role, seeded));
    }

    [Fact]
    public void Every_seed_user_password_satisfies_the_identity_password_policy()
    {
        // Mirrors AddIdentityCore options: length 10, 4 unique chars, digit, upper, lower, non-alphanumeric.
        foreach (var user in UsersFile)
        {
            var p = user.Password;
            var ok = p.Length >= 10
                     && p.Distinct().Count() >= 4
                     && p.Any(char.IsDigit)
                     && p.Any(char.IsUpper)
                     && p.Any(char.IsLower)
                     && p.Any(c => !char.IsLetterOrDigit(c));

            Assert.True(ok, $"The password of seed user '{user.Email}' does not satisfy the Identity password policy.");
        }
    }
}
