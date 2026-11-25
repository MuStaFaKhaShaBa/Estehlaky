using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.Infrastructure.Seed;

public static class SeedExtensions
{
    /// <summary>Call from AddInfrastructure. A new entity = new JSON file + new seeder + one line here.</summary>
    public static IServiceCollection AddSeeding(this IServiceCollection services)
    {
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    /// <summary>Call from Program.cs after Build().</summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(ct);
    }
}
