using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Tests.Infrastructure;
using WebApi.Middleware;

namespace Tests.Integrations.Socium;

/// <summary>
/// Временная БД с миграциями и DI как в WebApi. Каждый вызов сервиса — в новом scope (как отдельный HTTP-запрос).
/// </summary>
public sealed class SociumServiceFixture : IAsyncLifetime
{
    private PostgresTestHost? postgres;
    private ServiceProvider? provider;

    public async ValueTask InitializeAsync()
    {
        postgres = await PostgresTestHost.StartAsync("socium_service");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Socium"] = postgres.ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextExt(configuration);
        services.AddDependencyInjectionExt();
        provider = services.BuildServiceProvider(validateScopes: true);

        var factory = provider.GetRequiredService<IDbContextFactory<DbContextSocium>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    public async Task<T> RunAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = provider!.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public async Task<Guid> CreateRoomAsync(string name)
    {
        var created = await RunAsync((IServiceRoom service) => service.CreateAsync(new RoomCreateRequest { Name = name }));
        EnsureSaved(created, $"комнату «{name}»");

        var list = await RunAsync((IServiceRoom service) => service.DisplayListPageAsync(new RoomListPageRequest()));
        return list.Response!.Rows.Single(row => row.Name == name.Trim()).Id;
    }

    public async Task<Guid> CreateChatAsync(Guid roomId, string name)
    {
        var created = await RunAsync((IServiceChat service) => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = name }));
        EnsureSaved(created, $"чат «{name}»");

        var list = await RunAsync((IServiceChat service) => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));
        return list.Response!.Rows.Single(row => row.Name == name.Trim()).Id;
    }

    private static void EnsureSaved<T>(ResponseInfo<T> created, string subject) where T : class
    {
        if (created.MessageInfo.MessageType != MessageType.SAVED)
        {
            throw new InvalidOperationException($"Не удалось создать {subject}: {created.MessageInfo.MessageText}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (provider is not null)
        {
            await provider.DisposeAsync();
        }

        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class SociumServiceCollection : ICollectionFixture<SociumServiceFixture>
{
    public const string Name = "Socium.Services";
}
