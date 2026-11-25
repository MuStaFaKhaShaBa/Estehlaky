using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Estehlaky.API.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>Default requests per minute per IP on the "auth" policy.</summary>
    public const int DefaultAuthPermitLimit = 10;

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });

        // The limit is read from configuration when the options are first used (not while Program.cs runs),
        // so the integration tests can raise or lower it with RateLimiting:AuthPermitLimit.
        // Production keeps the default of 10 unless that key is set.
        services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((o, config) =>
        {
            var permitLimit = config.GetValue("RateLimiting:AuthPermitLimit", DefaultAuthPermitLimit);

            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // permitLimit requests per minute per IP on login / register / refresh / password endpoints.
            o.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }
}
