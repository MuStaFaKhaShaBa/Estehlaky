
using System.Text;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Common.Results;
using Estehlaky.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Estehlaky.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (bearer, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;

                    bearer.MapInboundClaims = false;

                    bearer.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwt.Issuer,

                            ValidateAudience = true,
                            ValidAudience = jwt.Audience,

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwt.Key)),

                            ValidAlgorithms =
                            [
                                SecurityAlgorithms.HmacSha256
                            ],

                            ValidateLifetime = true,
                            RequireExpirationTime = true,

                            ClockSkew = TimeSpan.FromSeconds(
                                jwt.ClockSkewSeconds),

                            NameClaimType = "sub"
                        };

                    bearer.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // Log the exception here if required.
                            return Task.CompletedTask;
                        },

                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            var error = context.AuthenticateFailure
                                switch
                            {
                                SecurityTokenExpiredException =>
                                    AuthErrors.ExpiredAccessToken,

                                null =>
                                    AuthErrors.TokenRequired,

                                _ =>
                                    AuthErrors.InvalidAccessToken
                            };

                            var result = Result.Failure(error);

                            var actionResult = result.ToProblem(
                                context.HttpContext);

                            await actionResult.ExecuteResultAsync(
                                new ActionContext(
                                    context.HttpContext,
                                    context.HttpContext.GetRouteData(),
                                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
                        },

                        OnForbidden = async context =>
                        {
                            var result = Result.Failure(
                                AuthErrors.Forbidden);

                            var actionResult = result.ToProblem(
                                context.HttpContext);

                            await actionResult.ExecuteResultAsync(
                                new ActionContext(
                                    context.HttpContext,
                                    context.HttpContext.GetRouteData(),
                                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
                        }
                    };
                });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());

        return services;
    }
}
