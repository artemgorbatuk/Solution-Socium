using Npgsql;
using Tests.Infrastructure;

namespace Tests.Integrations.Infrastructure;

public sealed class PostgresTestHostTests
{
    [Fact]
    public async Task Database_Start_WithAvailableServer_ShouldCreateReachableTemporaryDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var host = await PostgresTestHost.StartAsync("unithost", cancellationToken);

        await using var connection = new NpgsqlConnection(host.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT current_database()";

        Assert.Equal(host.DatabaseName, await command.ExecuteScalarAsync(cancellationToken));
        Assert.NotEqual("Socium", host.DatabaseName);
    }

    [Fact]
    public async Task Database_Dispose_WithStartedHost_ShouldDropDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var host = await PostgresTestHost.StartAsync("unithost", cancellationToken);
        var databaseName = host.DatabaseName;
        var adminConnectionString = new NpgsqlConnectionStringBuilder(host.ConnectionString) { Database = "postgres" }.ConnectionString;

        await host.DisposeAsync();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM pg_database WHERE datname = @name";
        command.Parameters.AddWithValue("name", databaseName);

        Assert.Equal(0L, await command.ExecuteScalarAsync(cancellationToken));
    }
}
