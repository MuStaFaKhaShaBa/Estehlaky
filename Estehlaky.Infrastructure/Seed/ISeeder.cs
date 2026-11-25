using Estehlaky.Application.Common.Results;

namespace Estehlaky.Infrastructure.Seed;

public interface ISeeder
{
    int Order { get; }

    string Name { get; }

    Task<Result> SeedAsync(CancellationToken ct);
}