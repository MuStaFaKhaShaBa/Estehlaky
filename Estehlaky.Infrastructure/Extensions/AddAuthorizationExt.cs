
using Estehlaky.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.Infrastructure.Extensions;

/// <summary>
/// Registers application authorization policies.
/// </summary>
public static class AddAuthorizationExt
{
    public static IServiceCollection AddAppAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(AuthPolicies.DataEntryOnly, policy => policy.RequireRole(Roles.DataEntry))
            .AddPolicy(AuthPolicies.ReviewerOnly, policy => policy.RequireRole(Roles.Reviewer))
            .AddPolicy(AuthPolicies.SupervisorOnly, policy => policy.RequireRole(Roles.Supervisor))
            .AddPolicy(AuthPolicies.Management, policy => policy.RequireRole(
                    Roles.Admin,
                    Roles.Manager,
                    Roles.Supervisor));

        return services;
    }
}
