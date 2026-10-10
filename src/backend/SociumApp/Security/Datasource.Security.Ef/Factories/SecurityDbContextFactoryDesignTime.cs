using Datasource.Security.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Datasource.Security.Ef.Factories;

/// <summary>
/// migration use; таблицы Security лежат в БД Socium, поэтому и строка подключения — Socium.
/// </summary>
public class SecurityDbContextFactoryDesignTime : IDesignTimeDbContextFactory<DbContextSecurity>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Socium";
    private const string LocalConnectionString = "Host=localhost;Port=5433;Database=Socium;Username=postgres;Password=postgres;";

    public DbContextSecurity CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<DbContextSecurity>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Security"));

        return new DbContextSecurity(optionsBuilder.Options);
    }
}
