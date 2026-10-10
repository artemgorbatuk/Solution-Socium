using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

public static class TestDatabase
{
    /// <summary>
    /// Порядок как в WebApi: миграция Socium Task0011 удаляет старую таблицу Users, Security создаёт новую.
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider services)
    {
        var sociumFactory = services.GetRequiredService<IDbContextFactory<DbContextSocium>>();
        await using (var socium = await sociumFactory.CreateDbContextAsync())
        {
            await socium.Database.MigrateAsync();
        }

        var securityFactory = services.GetRequiredService<IDbContextFactory<DbContextSecurity>>();
        await using (var security = await securityFactory.CreateDbContextAsync())
        {
            await security.Database.MigrateAsync();
        }
    }
}
