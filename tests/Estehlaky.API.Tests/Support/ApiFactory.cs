using Estehlaky.API;
using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Estehlaky.API.Tests.Support;

/// <summary>
/// Starts the real Estehlaky.API against the SQL Server container.
/// The connection string is NOT set here: AddInfrastructure reads it while Program.cs runs,
/// so <see cref="ApiFixture"/> provides it as the environment variable ConnectionStrings__Default.
/// </summary>
public sealed class ApiFactory(OffsetClock clock, CapturingEmailService emails) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestConfig.JwtKey,
            ["Jwt:Issuer"] = TestConfig.JwtIssuer,
            ["Jwt:Audience"] = TestConfig.JwtAudience,
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:ReuseGraceSeconds"] = "10",
            ["Jwt:MaxActiveSessions"] = "10",
            // Tolerates the clock being moved forward by a few seconds inside one test.
            ["Jwt:ClockSkewSeconds"] = "300",
            // Most tests make many auth calls from one "IP"; the rate-limit tests lower this on purpose.
            ["RateLimiting:AuthPermitLimit"] = "100000"
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(emails);
        });
    }
}
