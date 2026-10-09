using Datasource.Socium.Ef.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;

namespace Tests.Integrations.Socium;

/// <summary>
/// WebApi в памяти (окружение Testing) поверх временной БД с миграциями.
/// </summary>
public sealed class SociumApiFixture : IAsyncLifetime
{
    private PostgresTestHost? postgres;
    private WebApplicationFactory<Program>? factory;

    public HttpClient Client { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        postgres = await PostgresTestHost.StartAsync("socium_api");
        var connectionString = postgres.ConnectionString;

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Socium", connectionString);
        });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSocium>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
        }

        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();

        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class SociumApiCollection : ICollectionFixture<SociumApiFixture>
{
    public const string Name = "Socium.Api";
}
