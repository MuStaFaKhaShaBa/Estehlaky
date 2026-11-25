using System.Text.Json.Serialization;
using Estehlaky.API.Extensions;
using Estehlaky.API.Middleware;
using Estehlaky.API.Services;
using Estehlaky.Application;
using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Infrastructure;
using Estehlaky.Infrastructure.Data;
using Estehlaky.Infrastructure.Extensions;
using Estehlaky.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Estehlaky.API;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // Password-reset tokens are protected with these keys. In production, persist them
        // (file share, blob storage or database); otherwise a restart invalidates every pending reset.
        builder.Services.AddDataProtection();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddJwtAuthentication();
        builder.Services.AddAuthRateLimiting();
        builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecurityTransformer>());

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
        }
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(o => o.WithTitle("Estehlaky API")).AllowAnonymous();

        await app.Services.AddAutoMigrateDatabase();
        await app.Services.SeedDatabaseAsync();

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        await app.RunAsync();
    }
}
public partial class Program { }