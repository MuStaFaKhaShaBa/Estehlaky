using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Estehlaky.API.Extensions;

internal sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    // Add the exact model names as they appear in the Models list.
    private static readonly HashSet<string> Hidden = ["ProblemDetails", "ClientInfo"];

    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info = new OpenApiInfo { Title = "Estehlaky API", Version = "v1" };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken from /auth/login (without the word Bearer)."
            }
        };

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            }
        ];

        if (document.Components?.Schemas is { } schemas)
        {
            foreach (var name in schemas.Keys.Where(Hidden.Contains).ToList())
                schemas.Remove(name);
        }

        return Task.CompletedTask;
    }
}