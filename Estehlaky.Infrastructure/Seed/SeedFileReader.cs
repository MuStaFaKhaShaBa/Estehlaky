using System.Reflection;
using System.Text.Json;

namespace Estehlaky.Infrastructure.Seed;

/// <summary>
/// Reads JSON seed files embedded in the Infrastructure assembly.
/// </summary>
public static class SeedFileReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static List<T> Read<T>(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var assembly = typeof(SeedFileReader).Assembly;
        var assemblyName = assembly.GetName().Name;

        // Example: Estehlaky.Infrastructure.Seed.Data.roles.json
        var resourceName = $"{assemblyName}.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded seed resource '{resourceName}' was not found.");

        return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? [];
    }

    public static IEnumerable<string> GetResourceNames()
    {
        return typeof(SeedFileReader)
            .Assembly
            .GetManifestResourceNames();
    }
}