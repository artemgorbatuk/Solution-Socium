using Datasource.Files.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Datasource.Files.Ef.Factories;

/// <summary>
/// Таблицы Files лежат в БД Socium — строка подключения та же.
/// </summary>
public class FilesDbContextFactoryDesignTime : IDesignTimeDbContextFactory<DbContextFiles>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Socium";
    private const string LocalConnectionString = "Host=localhost;Port=5433;Database=Socium;Username=postgres;Password=postgres;";

    public DbContextFiles CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<DbContextFiles>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Files"));

        return new DbContextFiles(optionsBuilder.Options);
    }
}
