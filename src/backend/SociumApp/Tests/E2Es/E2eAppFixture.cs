using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Playwright;
using Services.Security.Models;
using Tests.Infrastructure;
using WebApi.Controllers.Shared;

namespace Tests.E2Es;

/// <summary>
/// Полный контур для E2E: временная БД, WebApi отдельным процессом (Development — сам применяет миграции),
/// собранный фронтенд за статическим сервером с прокси <c>/api</c> и Chromium в headless-режиме.
/// </summary>
public sealed class E2eAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan ApiStartTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan FrontendBuildTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FrontendStartTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Пароль всех пользователей, созданных фикстурой.</summary>
    public const string Password = "Password-123";

    private readonly ConcurrentDictionary<Guid, (string Login, string AccessToken)> users = new();
    private PostgresTestHost? postgres;
    private TestProcess? api;
    private TestProcess? frontend;
    private string? frontendOutputPath;
    private IPlaywright? playwright;
    private IBrowser? browser;

    /// <summary>Адрес фронтенда; запросы <c>/api</c> проксируются в WebApi.</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>Клиент к тому же WebApi через прокси фронтенда от имени <see cref="DefaultUserId"/> — для подготовки и проверки данных.</summary>
    public HttpClient Api { get; private set; } = default!;

    /// <summary>Логин и имя пользователя <see cref="DefaultUserId"/>.</summary>
    public const string DefaultUserLogin = "e2e.default";
    public const string DefaultUserName = "Пользователь по умолчанию";

    /// <summary>Пользователь, от которого по умолчанию работают <see cref="Api"/> и браузер: создатель и админ подготовленных чатов.</summary>
    public Guid DefaultUserId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        postgres = await PostgresTestHost.StartAsync("socium_e2e");

        var apiUrl = $"http://127.0.0.1:{GetFreePort()}";
        api = StartApi(repoRoot, apiUrl, postgres.ConnectionString);
        await WaitForUrlAsync($"{apiUrl}/api/user/create", api, ApiStartTimeout);

        var frontendDist = await BuildFrontendAsync(repoRoot);
        BaseUrl = $"http://127.0.0.1:{GetFreePort()}";
        frontend = TestProcess.Start(
            "node",
            $"\"{Path.Combine(repoRoot, "scripts", "e2e", "serve-frontend-static.mjs")}\" --port {new Uri(BaseUrl).Port} --dist \"{frontendDist}\" --api {apiUrl}",
            repoRoot);
        await WaitForUrlAsync(BaseUrl, frontend, FrontendStartTimeout);

        DefaultUserId = await CreateUserAsync(DefaultUserLogin, DefaultUserName);
        Api = CreateApiAs(DefaultUserId);
        playwright = await Playwright.CreateAsync();
        browser = await LaunchChromiumAsync(playwright);
    }

    /// <summary>Регистрирует пользователя с паролем <see cref="Password"/>, подтверждает email и запоминает access-токен.</summary>
    public async Task<Guid> CreateUserAsync(string login, string name, string? email = null)
    {
        email ??= $"{login.Trim().ToLowerInvariant()}@example.com";
        using var anonymous = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var created = await anonymous.PostAsJsonAsync(
            "/api/user",
            new UserCreateRequest { Email = email, Login = login, Name = name, Password = Password },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        await ConfirmEmailAsync(login);

        var loggedIn = await anonymous.PostAsJsonAsync("/api/account/login", new AccountLoginRequest { Login = login, Password = Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        var tokens = await loggedIn.Content.ReadFromJsonAsync<ApiSuccessResponse<AccountTokenResponseBody>>(TestContext.Current.CancellationToken);
        var userId = tokens!.Response!.User.Id;
        users[userId] = (login, tokens.Response.AccessToken);
        return userId;
    }

    /// <summary>Подтверждает email в БД без письма — для сценариев после регистрации в UI.</summary>
    public Task ConfirmEmailForLoginAsync(string login) => ConfirmEmailAsync(login);

    /// <summary>Назначает <c>RoomAdmin</c> в БД (для stub заявки после выхода из чата).</summary>
    public async Task GrantRoomAdminAsync(Guid roomId, Guid userId)
    {
        await using var connection = new Npgsql.NpgsqlConnection(postgres!.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new Npgsql.NpgsqlCommand(
            """
            INSERT INTO "RoomRoleAssignments" ("Id", "UserId", "RoomId", "RoleName")
            VALUES (@id, @userId, @roomId, @roleName)
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("roomId", roomId);
        command.Parameters.AddWithValue("roleName", Services.Security.Permissions.Catalogs.RoleNames.RoomAdmin);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task ConfirmEmailAsync(string login)
    {
        await using var connection = new Npgsql.NpgsqlConnection(postgres!.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new Npgsql.NpgsqlCommand(
            """UPDATE "Users" SET "IsEmailConfirmed" = TRUE WHERE "Login" = @login""",
            connection);
        command.Parameters.AddWithValue("login", login.Trim().ToLowerInvariant());
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Новый изолированный контекст браузера (свои cookies и localStorage), вошедший как <see cref="DefaultUserId"/>.</summary>
    public Task<IBrowserContext> NewContextAsync() => NewContextAsAsync(DefaultUserId);

    /// <summary>
    /// Новый изолированный контекст браузера, вошедший как <paramref name="userId"/>; <c>null</c> — без входа.
    /// Вход — запросом со страницы того же origin: refresh-cookie браузер сохраняет по своим правилам, как в приложении.
    /// </summary>
    public async Task<IBrowserContext> NewContextAsAsync(Guid? userId)
    {
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, Locale = "ru-RU" });
        if (userId is Guid id)
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("/favicon.ico");
            var status = await page.EvaluateAsync<int>(
                @"async ([login, password]) => (await fetch('/api/account/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ login, password, rememberMe: false }),
                })).status",
                new[] { users[id].Login, Password });
            Assert.Equal(200, status);
            await page.CloseAsync();
        }

        return context;
    }

    /// <summary>Клиент к WebApi с access-токеном пользователя, созданного через <see cref="CreateUserAsync"/>; вызывающий освобождает его.</summary>
    public HttpClient CreateApiAs(Guid userId)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", users[userId].AccessToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
        Api?.Dispose();
        frontend?.Dispose();
        api?.Dispose();

        if (frontendOutputPath is not null && Directory.Exists(frontendOutputPath))
        {
            try
            {
                Directory.Delete(frontendOutputPath, recursive: true);
            }
            catch
            {
                // ignore cleanup races
            }
        }

        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }

    private static TestProcess StartApi(string repoRoot, string apiUrl, string connectionString)
    {
        var webApiDir = Path.Combine(repoRoot, "src", "backend", "SociumApp", "WebApi");
        var webApiDll = Path.Combine(webApiDir, "bin", BuildConfiguration(), "net10.0", "WebApi.dll");
        if (!File.Exists(webApiDll))
        {
            throw new InvalidOperationException($"Не найден {webApiDll}. Соберите решение: dotnet build src/backend/SociumApp/Solution-Socium.slnx");
        }

        return TestProcess.Start(
            "dotnet",
            $"exec \"{webApiDll}\" --urls {apiUrl}",
            webApiDir,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ConnectionStrings__Socium"] = connectionString,
                ["Security__Jwt__SigningKey"] = TestJwt.CreateSigningKey(),
                // Access-токены клиентов фикстуры не обновляются — живут дольше прогона.
                ["Security__Jwt__AccessTokenMinutes"] = "240",
                // Все входы идут с одного IP.
                ["Security__RateLimit__PermitLimit"] = "100000",
                ["Messaging__RateLimit__PermitLimit"] = "100000",
                // Без Mailpit регистрация не должна ждать десятки секунд на SMTP.
                ["Messaging__Smtp__TimeoutSeconds"] = "3",
                ["Files__RootPath"] = Path.Combine(Path.GetTempPath(), $"socium-e2e-files-{Guid.CreateVersion7():N}"),
            });
    }

    private async Task<string> BuildFrontendAsync(string repoRoot)
    {
        var frontendDir = Path.Combine(repoRoot, "src", "frontend", "SociumWeb");
        var ngCli = Path.Combine(frontendDir, "node_modules", "@angular", "cli", "bin", "ng.js");
        if (!File.Exists(ngCli))
        {
            throw new InvalidOperationException($"Не установлены зависимости фронтенда. Выполните: npm install (в {frontendDir})");
        }

        frontendOutputPath = Path.Combine(Path.GetTempPath(), $"socium-e2e-{Guid.CreateVersion7():N}");
        await TestProcess.RunAsync(
            "node",
            $"\"{ngCli}\" build --configuration development --output-path \"{frontendOutputPath}\"",
            frontendDir,
            FrontendBuildTimeout);

        return Path.Combine(frontendOutputPath, "browser");
    }

    /// <summary>При первом запуске Chromium ещё не скачан — ставим его через встроенный CLI Playwright.</summary>
    private static async Task<IBrowser> LaunchChromiumAsync(IPlaywright playwright)
    {
        var options = new BrowserTypeLaunchOptions { Headless = true };
        try
        {
            return await playwright.Chromium.LaunchAsync(options);
        }
        catch (PlaywrightException)
        {
            var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Не удалось установить Chromium для Playwright (код {exitCode}). Проверьте доступ в интернет и повторите запуск тестов.");
            }

            return await playwright.Chromium.LaunchAsync(options);
        }
    }

    private static async Task WaitForUrlAsync(string url, TestProcess owner, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (owner.HasExited)
            {
                throw new InvalidOperationException($"Процесс завершился до готовности {url}:{Environment.NewLine}{owner.Output}");
            }

            try
            {
                using var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                // not listening yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{url} не ответил за {timeout}:{Environment.NewLine}{owner.Output}");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    /// <summary>Конфигурация сборки тестов (Debug/Release) — WebApi собран в ту же.</summary>
    private static string BuildConfiguration() =>
        new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "frontend")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"Не найден корень репозитория (global.json + src/frontend) выше {AppContext.BaseDirectory}");
    }
}

[CollectionDefinition(Name)]
public sealed class E2eCollection : ICollectionFixture<E2eAppFixture>
{
    public const string Name = "E2E";
}
