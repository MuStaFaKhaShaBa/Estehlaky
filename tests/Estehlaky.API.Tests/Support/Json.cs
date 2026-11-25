using System.Text.Json;
using System.Text.Json.Serialization;

namespace Estehlaky.API.Tests.Support;

internal static class Json
{
    /// <summary>Same shape the API uses: camelCase and enums as strings.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
