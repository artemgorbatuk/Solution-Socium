using System.Net.Http.Json;
using Datasource.Socium.Ef.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Socium.Models;
using Tests.Infrastructure;
using WebApi.Controllers.Shared;
using WebApi.Middleware;

namespace Tests.Integrations.Socium;

/// <summary>
/// WebApi в памяти (окружение Testing) поверх временной БД с миграциями.
/// <see cref="Client"/> отправляет запросы от имени пользователя по умолчанию.
/// </summary>
public sealed class SociumApiFixture : IAsyncLifetime
{
    private const string DefaultUserLogin = "default-user";

    private PostgresTestHost? postgres;
    private WebApplicationFactory<Program>? factory;

    public HttpClient Client { get; private set; } = default!;

    public Guid DefaultUserId { get; private set; }

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

        Client = CreateClientAs(null);
        DefaultUserId = await CreateUserAsync(Client, DefaultUserLogin, "Пользователь по умолчанию");
        Client.DefaultRequestHeaders.Add(CurrentUserFromHeader.HeaderName, DefaultUserId.ToString());
    }

    /// <summary>Клиент от имени пользователя <paramref name="userId"/>; <c>null</c> — без заголовка текущего пользователя.</summary>
    public HttpClient CreateClientAs(Guid? userId)
    {
        var client = factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (userId is Guid id)
        {
            client.DefaultRequestHeaders.Add(CurrentUserFromHeader.HeaderName, id.ToString());
        }

        return client;
    }

    public async Task<Guid> CreateUserAsync(HttpClient client, string login, string name)
    {
        var created = await client.PostAsJsonAsync("/api/user", new UserCreateRequest { Login = login, Name = name });
        created.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<ApiSuccessResponse<UserListPageResponse>>("/api/user");
        return list!.Response!.Rows.Single(row => row.Login == login).Id;
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
