using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Services.Security.Models;
using Services.Security.Texts;
using Services.Shared.Enums;
using Tests.Infrastructure;
using WebApi.Controllers.Shared;
using WebApi.Middleware;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Security;

[Collection(SecurityApiCollection.Name)]
public sealed class AccountApiTests(SecurityApiFixture fixture)
{
    private const string BaseUrl = "/api/account";
    private const string Password = SecurityApiFixture.DefaultPassword;

    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password = Password, bool rememberMe = false)
        => client.PostAsJsonAsync($"{BaseUrl}/login", new AccountLoginRequest { Login = login, Password = password, RememberMe = rememberMe }, TestContext.Current.CancellationToken);

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        return Assert.Single(cookies, cookie => cookie.StartsWith($"{RefreshTokenCookie.Name}="));
    }

    [Fact]
    public async Task Login_POST_WithValidCredentials_ShouldReturnAccessTokenAndSetProtectedSessionCookie()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateAnonymousClient();

        var response = await LoginAsync(client, login);

        var body = await ReadSuccessAsync<AccountTokenResponseBody>(response);
        Assert.Equal(id, body.Response!.User.Id);
        Assert.False(string.IsNullOrEmpty(body.Response.AccessToken));
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        var cookie = GetRefreshCookie(response).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains($"path={RefreshTokenCookie.Path}", cookie);
        Assert.DoesNotContain("expires=", cookie);
    }

    [Fact]
    public async Task Login_POST_WithRememberMe_ShouldSetPersistentCookie()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateAnonymousClient();

        var response = await LoginAsync(client, login, rememberMe: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("expires=", GetRefreshCookie(response).ToLowerInvariant());
    }

    [Fact]
    public async Task Login_POST_WithWrongPassword_ShouldReturn401WithoutCookie()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateAnonymousClient();

        var response = await LoginAsync(client, login, "wrong-password");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Contains(AccountTexts.Messages.Validation.InvalidCredentials, problem.Detail);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_POST_WithCookieFromLogin_ShouldReturnNewAccessTokenAndRotateCookie()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateAnonymousClient();
        var loggedIn = await LoginAsync(client, login);
        var firstCookie = GetRefreshCookie(loggedIn);

        var response = await client.PostAsync($"{BaseUrl}/refresh", null, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<AccountTokenResponseBody>(response);
        Assert.False(string.IsNullOrEmpty(body.Response!.AccessToken));
        Assert.NotEqual(firstCookie, GetRefreshCookie(response));
    }

    [Fact]
    public async Task Refresh_POST_WithoutCookie_ShouldReturn401()
    {
        using var client = fixture.CreateAnonymousClient();

        var response = await client.PostAsync($"{BaseUrl}/refresh", null, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Contains(AccountTexts.Messages.Validation.RefreshTokenInvalid, problem.Detail);
    }

    [Fact]
    public async Task Logout_POST_WithCookie_ShouldDeleteCookieAndRefreshShouldReturn401()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateAnonymousClient();
        await LoginAsync(client, login);

        var logout = await client.PostAsync($"{BaseUrl}/logout", null, TestContext.Current.CancellationToken);

        await ReadSuccessAsync<AccountLogoutResponse>(logout);
        Assert.Contains("expires=thu, 01 jan 1970", GetRefreshCookie(logout).ToLowerInvariant());
        var refresh = await client.PostAsync($"{BaseUrl}/refresh", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Profile_GETAndPUT_WithAccessToken_ShouldReturnAndUpdateOwnName()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        using var client = fixture.CreateClientAs(id);

        var update = await client.PutAsJsonAsync($"{BaseUrl}/profile", new AccountProfileUpdateRequest { Name = "Иван Петров" }, TestContext.Current.CancellationToken);
        var profile = await ReadSuccessAsync<AccountProfileResponse>(await client.GetAsync($"{BaseUrl}/profile", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, (await ReadSuccessAsync<AccountProfileResponse>(update)).MessageInfo.MessageType);
        Assert.Equal(id, profile.Response!.Id);
        Assert.Equal(login, profile.Response.Login);
        Assert.Equal("Иван Петров", profile.Response.Name);
    }

    [Fact]
    public async Task Profile_GET_WithoutOrWithForeignToken_ShouldReturn401Problem()
    {
        using var anonymous = fixture.CreateAnonymousClient();
        using var forged = fixture.CreateAnonymousClient();
        forged.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var anonymousResponse = await anonymous.GetAsync($"{BaseUrl}/profile", TestContext.Current.CancellationToken);
        var forgedResponse = await forged.GetAsync($"{BaseUrl}/profile", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(anonymousResponse, HttpStatusCode.Unauthorized);
        Assert.Equal(AuthenticationRegistration.UnauthorizedDetail, problem.Detail);
        await ReadProblemAsync(forgedResponse, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_POST_OverRateLimit_ShouldReturn429()
    {
        await using var limited = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("Security:RateLimit:PermitLimit", "2"));
        using var client = limited.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var login = UniqueLogin();

        var first = await LoginAsync(client, login);
        var second = await LoginAsync(client, login);
        var third = await LoginAsync(client, login);

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
