using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Services.Messaging.Providers;
using Services.Security.Models;
using Services.Security.Tokens.Providers;
using WebApi.Controllers.Shared;

namespace Tests.Infrastructure;

/// <summary>
/// WebApi в памяти (окружение Testing) поверх временной БД с миграциями.
/// <see cref="Client"/> вошёл как пользователь по умолчанию через <c>/api/account/login</c>.
/// Базовый адрес https: refresh-cookie помечена Secure и по http не отправляется.
/// </summary>
public abstract class ApiFixtureBase : IAsyncLifetime
{
    public const string DefaultPassword = ServiceFixtureBase.DefaultPassword;

    private static readonly Uri BaseAddress = new("https://localhost");

    private readonly string databasePrefix;
    private PostgresTestHost? postgres;

    protected ApiFixtureBase(string databasePrefix)
    {
        this.databasePrefix = databasePrefix;
    }

    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

    public HttpClient Client { get; private set; } = default!;

    public Guid DefaultUserId { get; private set; }

    public CapturingMessageChannel Mail => Factory.Services.GetRequiredService<CapturingMessageChannel>();

    public async ValueTask InitializeAsync()
    {
        postgres = await PostgresTestHost.StartAsync(databasePrefix);
        var connectionString = postgres.ConnectionString;
        var signingKey = TestJwt.CreateSigningKey();

        // Лимит входа поднят: все тесты идут с одного «IP».
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Socium", connectionString);
            builder.UseSetting(TestJwt.SigningKeySetting, signingKey);
            builder.UseSetting("Security:RateLimit:PermitLimit", "100000");
            builder.UseSetting("Messaging:RateLimit:PermitLimit", "100000");
        });

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            await TestDatabase.MigrateAsync(scope.ServiceProvider);
        }

        DefaultUserId = await CreateUserAsync(ServiceFixtureBase.DefaultUserLogin, ServiceFixtureBase.DefaultUserName, ServiceFixtureBase.DefaultUserEmail);
        Client = CreateAnonymousClient();
        var login = await Client.PostAsJsonAsync("/api/account/login", new AccountLoginRequest { Login = ServiceFixtureBase.DefaultUserLogin, Password = DefaultPassword });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<ApiSuccessResponse<AccountTokenResponseBody>>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.Response!.AccessToken);
    }

    /// <summary>Клиент без токена; cookie хранит сам (refresh-cookie после входа).</summary>
    public HttpClient CreateAnonymousClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = BaseAddress,
        });
    }

    /// <summary>Клиент с access-токеном пользователя <paramref name="userId"/>; <c>null</c> — без токена.</summary>
    public HttpClient CreateClientAs(Guid? userId)
    {
        var client = CreateAnonymousClient();
        if (userId is Guid id)
        {
            var token = Factory.Services.GetRequiredService<IAccessTokenProvider>().Create(id);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        return client;
    }

    /// <summary>Регистрирует пользователя, подтверждает email и возвращает Id.</summary>
    public async Task<Guid> CreateUserAsync(string login, string name, string? email = null, string password = DefaultPassword)
    {
        email ??= $"{login.Trim().ToLowerInvariant()}@example.com";
        using var client = CreateAnonymousClient();
        var created = await client.PostAsJsonAsync("/api/user", new UserCreateRequest
        {
            Email = email,
            Login = login,
            Name = name,
            Password = password,
        });
        created.EnsureSuccessStatusCode();

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<Repositories.Security.Ef.Api.IUnitOfWorkSecurity>();
            var user = await unitOfWork.Users.GetSingleByLoginOrDefaultAsync(login.Trim().ToLowerInvariant());
            user!.IsEmailConfirmed = true;
            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();
        }

        var loggedIn = await client.PostAsJsonAsync("/api/account/login", new AccountLoginRequest { Login = login, Password = password });
        loggedIn.EnsureSuccessStatusCode();
        var tokens = await loggedIn.Content.ReadFromJsonAsync<ApiSuccessResponse<AccountTokenResponseBody>>();
        return tokens!.Response!.User.Id;
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }
}

/// <summary>
/// Тело ответа входа и обновления, как его видит клиент: refresh-токена в нём нет.
/// </summary>
public sealed class AccountTokenResponseBody
{
    public required string AccessToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required AccountProfileResponse User { get; init; }
}
