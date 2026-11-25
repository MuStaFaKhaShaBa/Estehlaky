using Estehlaky.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Estehlaky.Infrastructure.Extensions
{
    public class AppDbContextFactory
        : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            // Find the API project configuration directory.
            var possiblePaths = new[]
            {
                currentDirectory,
                Path.Combine(currentDirectory, "Estehlaky.API"),
                Path.Combine(currentDirectory, "..", "Estehlaky.API")
            };

            var configurationPath = possiblePaths
                .Select(Path.GetFullPath)
                .FirstOrDefault(path =>
                    File.Exists(Path.Combine(path, "appsettings.json")));

            if (configurationPath == null)
            {
                throw new DirectoryNotFoundException(
                    "Could not find the API appsettings.json file.");
            }

            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(configurationPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(
                    $"appsettings.{environment}.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            // The application reads "Default"; "DefaultConnection" is kept as a fallback for older setups.
            var connectionString =
                configuration.GetConnectionString("Default")
                ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Default' is missing from configuration.");
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}