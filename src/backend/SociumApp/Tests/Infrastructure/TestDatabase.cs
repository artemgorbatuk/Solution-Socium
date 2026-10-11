using Datasource.Files.Ef.Contexts;
using Datasource.Messaging.Ef.Contexts;
using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

public static class TestDatabase
{
    /// <summary>
    /// Порядок как в WebApi: Socium → Security → Messaging → Files.
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

        var messagingFactory = services.GetRequiredService<IDbContextFactory<DbContextMessaging>>();
        await using (var messaging = await messagingFactory.CreateDbContextAsync())
        {
            await messaging.Database.MigrateAsync();
        }

        var filesFactory = services.GetRequiredService<IDbContextFactory<DbContextFiles>>();
        await using (var files = await filesFactory.CreateDbContextAsync())
        {
            await files.Database.MigrateAsync();
        }
    }
}
