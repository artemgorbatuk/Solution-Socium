using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Playwright;
using Tests.E2Es.Socium;
using Tests.Infrastructure;

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
    private const string CurrentUserHeader = "X-User-Id";
    private const string CurrentUserStorageKey = "socium.currentUserId";

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

    /// <summary>Имя пользователя <see cref="DefaultUserId"/>.</summary>
    public const string DefaultUserName = "Пользователь по умолчанию";

    /// <summary>Пользователь, от которого по умолчанию работают <see cref="Api"/> и браузер: создатель и админ подготовленных чатов.</summary>
    public Guid DefaultUserId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        postgres = await PostgresTestHost.StartAsync("socium_e2e");

        var apiUrl = $"http://127.0.0.1:{GetFreePort()}";
        api = StartApi(repoRoot, apiUrl, postgres.ConnectionString);
        await WaitForUrlAsync($"{apiUrl}/api/room", api, ApiStartTimeout);

        var frontendDist = await BuildFrontendAsync(repoRoot);
        BaseUrl = $"http://127.0.0.1:{GetFreePort()}";
        frontend = TestProcess.Start(
            "node",
            $"\"{Path.Combine(repoRoot, "scripts", "e2e", "serve-frontend-static.mjs")}\" --port {new Uri(BaseUrl).Port} --dist \"{frontendDist}\" --api {apiUrl}",
            repoRoot);
        await WaitForUrlAsync(BaseUrl, frontend, FrontendStartTimeout);

        Api = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        DefaultUserId = await Api.CreateUserAsync("e2e.default", DefaultUserName);
        Api.DefaultRequestHeaders.Add(CurrentUserHeader, DefaultUserId.ToString());
        playwright = await Playwright.CreateAsync();
        browser = await LaunchChromiumAsync(playwright);
    }

    /// <summary>Новый изолированный контекст браузера (свои cookies и localStorage) с выбранным <see cref="DefaultUserId"/>.</summary>
    public Task<IBrowserContext> NewContextAsync() => NewContextAsAsync(DefaultUserId);

    /// <summary>Новый изолированный контекст браузера с выбранным пользователем; <c>null</c> — пользователь не выбран.</summary>
    public Task<IBrowserContext> NewContextAsAsync(Guid? userId)
    {
        var localStorage = userId is Guid id ? new[] { new { name = CurrentUserStorageKey, value = id.ToString() } } : [];
        var storageState = JsonSerializer.Serialize(new { cookies = Array.Empty<object>(), origins = new[] { new { origin = BaseUrl, localStorage } } });
        return browser!.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, Locale = "ru-RU", StorageState = storageState });
    }

    /// <summary>Клиент к WebApi от имени другого пользователя; вызывающий освобождает его.</summary>
    public HttpClient CreateApiAs(Guid userId)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Add(CurrentUserHeader, userId.ToString());
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
