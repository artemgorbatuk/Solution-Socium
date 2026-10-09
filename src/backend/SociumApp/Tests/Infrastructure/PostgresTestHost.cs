using System.Net.Sockets;
using Npgsql;

namespace Tests.Infrastructure;

/// <summary>
/// Временная БД для Integrations/E2E на уже запущенном локальном PostgreSQL (socium-dev-postgres).
/// Docker-контейнеры не поднимает: если сервер недоступен, падает с подсказкой.
/// </summary>
public sealed class PostgresTestHost : IAsyncDisposable
{
    private const string LocalTemplate = "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=postgres";
    private const string StartHint = "Запустите его из корня репозитория: docker compose -f deploy/development/docker-compose.yml up -d";

    private readonly string adminConnectionString;

    private PostgresTestHost(string connectionString, string adminConnectionString, string databaseName)
    {
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        this.adminConnectionString = adminConnectionString;
    }

    public string ConnectionString { get; }
    public string DatabaseName { get; }

    /// <summary>
    /// Шаблон подключения: env <c>TEST_POSTGRES</c>, затем <c>ConnectionStrings__Socium</c>, иначе <c>localhost:5433</c>.
    /// Имя БД в шаблоне игнорируется — создаётся временная БД <c>{prefix}_{guid}</c>.
    /// </summary>
    public static async Task<PostgresTestHost> StartAsync(string databaseNamePrefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseNamePrefix);

        var template = Environment.GetEnvironmentVariable("TEST_POSTGRES")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Socium")
            ?? LocalTemplate;

        var adminConnectionString = ToAdminConnectionString(template);
        var databaseName = $"{SanitizePrefix(databaseNamePrefix)}_{Guid.CreateVersion7():N}";

        try
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"""CREATE DATABASE "{databaseName}";""";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is NpgsqlException or SocketException or TimeoutException)
        {
            var host = new NpgsqlConnectionStringBuilder(adminConnectionString);
            throw new InvalidOperationException(
                $"PostgreSQL для тестов недоступен ({host.Host}:{host.Port}). {StartHint}",
                exception);
        }

        var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        }.ConnectionString;

        return new PostgresTestHost(connectionString, adminConnectionString, databaseName);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""DROP DATABASE IF EXISTS "{DatabaseName}" WITH (FORCE);""";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // ignore cleanup races
        }
    }

    private static string ToAdminConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };
        return builder.ConnectionString;
    }

    private static string SanitizePrefix(string prefix)
    {
        var chars = prefix
            .Select(ch => char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')
            .ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "test" : sanitized;
    }
}
