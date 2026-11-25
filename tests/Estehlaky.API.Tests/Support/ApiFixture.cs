using Estehlaky.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Estehlaky.API.Tests.Support;

/// <summary>
/// One SQL Server database and one running API for the whole test run.
/// Tests stay independent by registering their own users (unique email per test).
///
/// The SQL Server comes from the environment variable ESTEHLAKY_TEST_SQL (a connection string WITHOUT a database).
/// A throwaway database named EstehlakyTests_&lt;random&gt; is created on it and dropped at the end;
/// your real databases are never touched.
///   - locally: your own SQL Server, e.g. "Server=.;Trusted_Connection=True;TrustServerCertificate=True"
///   - in CI:   the SQL Server service container defined in ci.yml
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    /// <summary>Server connection string without a database.</summary>
    public const string LocalSqlVariable = "ESTEHLAKY_TEST_SQL";

    private string _serverConnection = "";
    private string _databaseName = "";

    public OffsetClock Clock { get; } = new();
    public CapturingEmailService Emails { get; } = new();
    public ApiFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable(LocalSqlVariable);
        if (string.IsNullOrWhiteSpace(server))
        {
            throw new InvalidOperationException(
                $"Set the environment variable {LocalSqlVariable} to a SQL Server connection string without a database, " +
                "for example: Server=.;Trusted_Connection=True;TrustServerCertificate=True");
        }

        _serverConnection = server;
        _databaseName = $"EstehlakyTests_{Guid.NewGuid():N}";
        var serverConnection = server;

        var connectionString = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = _databaseName
        }.ConnectionString;

        // Program.cs seeds the database on startup but never migrates it, so migrate first.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "core"))
            .Options;

        await using (var db = new AppDbContext(options))
            await db.Database.MigrateAsync();

        // AddInfrastructure reads the connection string while Program.cs runs, so it must be an environment variable.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);

        // Same settings again as environment variables: visible no matter when the host reads its configuration.
        foreach (var (key, value) in TestConfig.StartupSettings)
            Environment.SetEnvironmentVariable(key.Replace(":", "__"), value);

        Factory = new ApiFactory(Clock, Emails);
        using var warmUp = Factory.CreateClient();   // starts the host (and runs the seeders)
    }

    public ApiClient CreateClient() =>
        new(Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();

        if (_databaseName.Length > 0) await DropDatabaseAsync();
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            SqlConnection.ClearAllPools();   // otherwise pooled connections keep the database busy

            var master = new SqlConnectionStringBuilder(_serverConnection) { InitialCatalog = "master" }.ConnectionString;
            await using var connection = new SqlConnection(master);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // Cleanup problems must not fail a test run; tell the developer which database to delete by hand.
            Console.WriteLine($"Could not drop test database '{_databaseName}': {ex.Message}");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
