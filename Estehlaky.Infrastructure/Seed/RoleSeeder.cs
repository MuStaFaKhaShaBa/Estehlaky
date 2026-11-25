using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Common.Results.ErrorCatalog;
using Estehlaky.Domain.Core.IdentitySchema;
using Estehlaky.Infrastructure.Seed.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estehlaky.Infrastructure.Seed
{
    public sealed class RoleSeeder(
        RoleManager<AppRole> roles,
        ILogger<RoleSeeder> logger) : ISeeder
    {
        public int Order => 1;
        public string Name => "Roles";

        public async Task<Result> SeedAsync(CancellationToken ct)
        {
            // Fetch first: if the table already has any role, ignore the seed file completely.
            if (await roles.Roles.AnyAsync(ct))
            {
                logger.LogInformation("Roles already exist. Skipping role seeding.");
                return Result.Success();
            }

            var items = SeedFileReader.Read<SeedRole>("Seed.Data.roles.json")
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .DistinctBy(r => r.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                var identityResult = await roles.CreateAsync(new AppRole
                {
                    Id = Guid.CreateVersion7(),
                    Name = item.Name
                });

                var result = identityResult.ToResult(SeedErrors.RoleCreationFailed);

                if (result.IsFailure)
                {
                    logger.LogError(
                        "Failed to seed role {Role}. Errors: {Errors}",
                        item.Name,
                        string.Join("; ", result.Errors.Select(e => e.Description)));

                    return result;
                }

                logger.LogInformation("Seeded role {Role}", item.Name);
            }

            return Result.Success();
        }
    }
}