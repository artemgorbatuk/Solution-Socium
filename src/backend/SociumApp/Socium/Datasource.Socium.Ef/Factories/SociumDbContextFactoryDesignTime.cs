using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Datasource.Socium.Ef.Factories;

/// <summary>
/// migration use
/// </summary>
public class SociumDbContextFactoryDesignTime : IDesignTimeDbContextFactory<DbContextSocium>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Socium";
    private const string LocalConnectionString = "Host=localhost;Port=5433;Database=Socium;Username=postgres;Password=postgres;";

    public DbContextSocium CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<DbContextSocium>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Socium"));

        return new DbContextSocium(optionsBuilder.Options);
    }
}
