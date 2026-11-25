using Estehlaky.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.Infrastructure.Extensions;

public static class AddAutoMigrateDBExt
{
    public static async Task<IServiceProvider> AddAutoMigrateDatabase(
        this IServiceProvider services,
        CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // await db.Database.MigrateAsync(ct);

        return services;
    }
}