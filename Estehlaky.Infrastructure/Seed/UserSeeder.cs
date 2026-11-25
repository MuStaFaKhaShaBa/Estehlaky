using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Common.Results.ErrorCatalog;
using Estehlaky.Domain.Core.IdentitySchema;
using Estehlaky.Infrastructure.Seed.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Estehlaky.Infrastructure.Seed
{
    public sealed class UserSeeder(
        UserManager<AppUser> users,
        IHostEnvironment environment,
        ILogger<UserSeeder> logger) : ISeeder
    {
        public int Order => 2;
        public string Name => "Users";

        public async Task<Result> SeedAsync(CancellationToken ct)
        {
            // Fetch first: if the table already has any user, ignore the seed file completely.
            if (await users.Users.AnyAsync(ct))
            {
                logger.LogInformation("Users already exist. Skipping user seeding.");
                return Result.Success();
            }

            var items = SeedFileReader.Read<SeedUser>("Seed.Data.users.json")
                .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                .Where(u => !u.DevelopmentOnly || environment.IsDevelopment())
                .DistinctBy(u => u.Email, StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(item.Password))
                {
                    logger.LogError("Missing password for seed user {Email}.", item.Email);
                    return Result.Failure(SeedErrors.InvalidConfiguration);
                }

                var user = new AppUser
                {
                    Id = Guid.CreateVersion7(),
                    UserName = item.Email,
                    Email = item.Email,
                    EmailConfirmed = true,
                    FullName = $"{item.FirstName} {item.LastName}".Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await users.CreateAsync(user, item.Password);
                var result = createResult.ToResult(SeedErrors.UserCreationFailed);

                if (result.IsFailure)
                {
                    logger.LogError(
                        "Failed to create seed user {Email}: {Errors}",
                        item.Email,
                        string.Join("; ", result.Errors.Select(e => e.Code)));

                    return result;
                }

                logger.LogInformation("Seeded user {Email}", item.Email);

                var roleNames = item.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (roleNames.Count == 0)
                    continue;

                var assignmentResult = (await users.AddToRolesAsync(user, roleNames))
                    .ToResult(SeedErrors.RoleAssignmentFailed);

                if (assignmentResult.IsFailure)
                {
                    logger.LogError(
                        "Failed to assign roles to user {Email}: {Errors}",
                        item.Email,
                        string.Join("; ", assignmentResult.Errors.Select(e => e.Code)));

                    return assignmentResult;
                }

                logger.LogInformation(
                    "Assigned roles {Roles} to user {Email}",
                    string.Join(", ", roleNames),
                    item.Email);
            }

            return Result.Success();
        }
    }
}