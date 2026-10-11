using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Security.Ef.Api;
using Services.Messaging.Providers;
using Services.Security.Api;
using Services.Security.Models;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;
using WebApi.Middleware;

namespace Tests.Infrastructure;

/// <summary>
/// Временная БД с миграциями и DI как в WebApi. Каждый вызов сервиса — в новом scope (как отдельный HTTP-запрос)
/// от имени пользователя по умолчанию, если не указан другой.
/// </summary>
public abstract class ServiceFixtureBase : IAsyncLifetime
{
    public const string DefaultPassword = "Password-123";
    public const string DefaultUserLogin = "default-user";
    public const string DefaultUserEmail = "default-user@example.com";
    public const string DefaultUserName = "Пользователь по умолчанию";

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; set; }
    }

    private readonly string databasePrefix;
    private PostgresTestHost? postgres;
    private ServiceProvider? provider;

    protected ServiceFixtureBase(string databasePrefix)
    {
        this.databasePrefix = databasePrefix;
    }

    public Guid DefaultUserId { get; private set; }

    public CapturingMessageChannel Mail => provider!.GetRequiredService<CapturingMessageChannel>();

    public async ValueTask InitializeAsync()
    {
        postgres = await PostgresTestHost.StartAsync(databasePrefix);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Socium"] = postgres.ConnectionString,
                [TestJwt.SigningKeySetting] = TestJwt.CreateSigningKey(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextExt(configuration);
        services.AddDependencyInjectionExt(configuration, captureMail: true);
        services.AddScoped<TestCurrentUser>();
        services.AddScoped<ICurrentUser>(serviceProvider => serviceProvider.GetRequiredService<TestCurrentUser>());
        provider = services.BuildServiceProvider(validateScopes: true);

        await TestDatabase.MigrateAsync(provider);

        DefaultUserId = await CreateUserAsync(DefaultUserLogin, DefaultUserName, DefaultUserEmail);
    }

    public Task<T> RunAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
        => RunAsAsync(DefaultUserId, action);

    /// <summary>Вызов от имени пользователя <paramref name="userId"/>; <c>null</c> — без текущего пользователя.</summary>
    public async Task<T> RunAsAsync<TService, T>(Guid? userId, Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = provider!.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().UserId = userId;
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    /// <summary>Регистрирует пользователя без подтверждения email (письмо уходит в <see cref="Mail"/>).</summary>
    public async Task<(Guid Id, string Email)> CreateUnconfirmedUserAsync(string login, string name, string? email = null, string password = DefaultPassword)
    {
        email ??= $"{login.Trim().ToLowerInvariant()}@example.com";
        Mail.Clear();
        var created = await RunAsAsync(null, (IServiceUser service) => service.CreateAsync(new UserCreateRequest
        {
            Email = email,
            Login = login,
            Name = name,
            Password = password,
        }));
        EnsureSaved(created, $"пользователя «{login}»");

        var id = await RunAsAsync(null, async (IUnitOfWorkSecurity unitOfWork) =>
        {
            var user = await unitOfWork.Users.GetSingleByLoginOrDefaultAsync(login.Trim().ToLowerInvariant());
            return user!.Id;
        });
        return (id, email);
    }

    /// <summary>Регистрирует пользователя, подтверждает email и возвращает Id (без входа).</summary>
    public async Task<Guid> CreateUserAsync(string login, string name, string? email = null, string password = DefaultPassword)
    {
        var (id, _) = await CreateUnconfirmedUserAsync(login, name, email, password);
        await RunAsAsync(null, async (IUnitOfWorkSecurity unitOfWork) =>
        {
            var user = await unitOfWork.Users.GetSingleOrDefaultAsync(id);
            user!.IsEmailConfirmed = true;
            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();
            return 0;
        });
        return id;
    }

    protected static void EnsureSaved<T>(ResponseInfo<T> created, string subject) where T : class
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
