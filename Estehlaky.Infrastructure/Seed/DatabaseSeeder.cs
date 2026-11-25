using Microsoft.Extensions.Logging;

namespace Estehlaky.Infrastructure.Seed;

/// <summary>Runs every registered seeder in order.</summary>
public sealed class DatabaseSeeder(IEnumerable<ISeeder> seeders, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var seeder in seeders.OrderBy(s => s.Order))
        {
            logger.LogInformation("Seeding {Seeder}", seeder.Name);
            await seeder.SeedAsync(ct);
        }
    }
}
