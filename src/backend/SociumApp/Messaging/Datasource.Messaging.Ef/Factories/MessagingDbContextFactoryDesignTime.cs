using Datasource.Messaging.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Datasource.Messaging.Ef.Factories;

/// <summary>
/// Таблицы Messaging лежат в БД Socium — строка подключения та же.
/// </summary>
public class MessagingDbContextFactoryDesignTime : IDesignTimeDbContextFactory<DbContextMessaging>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Socium";
    private const string LocalConnectionString = "Host=localhost;Port=5433;Database=Socium;Username=postgres;Password=postgres;";

    public DbContextMessaging CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<DbContextMessaging>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Messaging"));

        return new DbContextMessaging(optionsBuilder.Options);
    }
}
