
using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Domain.Core.IdentitySchema;
using Estehlaky.Infrastructure.Auth;
using Estehlaky.Infrastructure.Data;
using Estehlaky.Infrastructure.Seed;
using Estehlaky.Infrastructure.Services;
using Estehlaky.Infrastructure.Sms;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentdm;

namespace Estehlaky.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // --------------------------------------------------
        // Configuration
        // --------------------------------------------------

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ResendOptions>()
            .Bind(config.GetSection(ResendOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey),
                "Resend API key is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.FromEmail),
                "Resend sender email is required.")
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(config.GetSection(OtpOptions.SectionName))
            .Validate(o => o.Length is >= 6 and <= 8,
                "OTP length must be between 6 and 8.")
            .Validate(o => o.ExpirationMinutes > 0,
                "OTP expiration must be positive.")
            .Validate(o => o.MaxAttempts > 0,
                "Maximum OTP attempts must be positive.")
            .Validate(o => o.ResendCooldownSeconds > 0,
                "OTP resend cooldown must be positive.")
            .Validate(o =>
            {
                try
                {
                    return Convert.FromBase64String(o.HashingKey).Length >= 32;
                }
                catch (FormatException)
                {
                    return false;
                }
            },
            "OTP hashing key must be valid Base64 containing at least 32 bytes.")
            .ValidateOnStart();

        // --------------------------------------------------
        // Database
        // --------------------------------------------------

        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' is missing.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "core");
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // --------------------------------------------------
        // ASP.NET Core Identity
        // --------------------------------------------------

        services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;

            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            options.Password.RequiredLength = 10;
            options.Password.RequiredUniqueChars = 4;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // --------------------------------------------------
        // Identity Token Configuration
        // --------------------------------------------------

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(1);
        });

        // --------------------------------------------------
        // Application Services
        // --------------------------------------------------

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<TokenService>();

        services.AddScoped<IAuthService, AuthService>();

        // --------------------------------------------------
        // Email Services
        // --------------------------------------------------

        services.AddScoped<IEmailService, ResendEmailService>();

        // --------------------------------------------------
        // SMS Services — Sent.dm SDK
        // --------------------------------------------------

        var sentApiKey = config["SentSms:ApiKey"];

        if (string.IsNullOrWhiteSpace(sentApiKey))
        {
            throw new InvalidOperationException(
                "Sent SMS API key is not configured.");
        }

        var sentClient = new SentClient
        {
            ApiKey = sentApiKey
        };

        services.AddSingleton<ISentClient>(sentClient);

        services.AddScoped<ISmsService, SentDmSmsService>();

        // --------------------------------------------------
        // Authentication Support Services
        // --------------------------------------------------

        services.AddScoped<
            IOtpService,
            OtpService>();

        // --------------------------------------------------
        // Database Seeding
        // --------------------------------------------------

        services.AddSeeding();

        return services;
    }
}
