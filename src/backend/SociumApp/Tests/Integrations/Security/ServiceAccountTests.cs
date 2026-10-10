using Datasource.Security.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Services.Security.Api;
using Services.Security.Models;
using Services.Security.Texts;
using Services.Security.Tokens.Providers;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Tests.Integrations.Security;

[Collection(SecurityServiceCollection.Name)]
public sealed class ServiceAccountTests(SecurityServiceFixture fixture)
{
    private const string Password = SecurityServiceFixture.DefaultPassword;

    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsAsync<T>(Guid? userId, Func<IServiceAccount, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsAsync(userId, action);

    private Task<ResponseInfo<AccountTokenResponse>> LoginAsync(string login, string password = Password, bool rememberMe = false)
        => RunAsAsync(null, service => service.LoginAsync(new AccountLoginRequest { Login = login, Password = password, RememberMe = rememberMe }));

    private Task<ResponseInfo<AccountTokenResponse>> RefreshAsync(string? refreshToken)
        => RunAsAsync(null, service => service.RefreshAsync(new AccountRefreshRequest { RefreshToken = refreshToken }));

    private Task<List<Datasource.Security.Ef.Models.RefreshToken>> GetTokensAsync(Guid userId)
        => fixture.RunAsAsync(null, (DbContextSecurity db) => db.RefreshTokens.AsNoTracking().Where(token => token.UserId == userId).OrderBy(token => token.CreatedAt).ToListAsync());

    [Fact]
    public async Task Login_Submit_WithValidCredentialsInOtherCase_ShouldReturnTokensAndProfile()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await LoginAsync($" {login.ToUpperInvariant()} ");

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.False(string.IsNullOrEmpty(result.Response!.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Response.RefreshToken));
        Assert.False(result.Response.IsPersistent);
        Assert.Equal(id, result.Response.User.Id);
        Assert.Equal(login, result.Response.User.Login);
        Assert.Equal("Иван", result.Response.User.Name);
    }

    [Fact]
    public async Task Login_Submit_WithValidCredentials_ShouldStoreOnlyRefreshTokenHash()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await LoginAsync(login, rememberMe: true);

        var token = Assert.Single(await GetTokensAsync(id));
        var hash = await fixture.RunAsAsync(null, (IRefreshTokenProvider provider) => Task.FromResult(provider.Hash(result.Response!.RefreshToken)));
        Assert.Equal(hash, token.TokenHash);
        Assert.NotEqual(result.Response!.RefreshToken, token.TokenHash);
        Assert.True(token.IsPersistent);
        Assert.Null(token.RevokedAt);
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("")]
    public async Task Login_Submit_WithWrongPassword_ShouldReturnUnauthorizedInvalidCredentials(string password)
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");

        var result = await LoginAsync(login, password);

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Contains(AccountTexts.Messages.Validation.InvalidCredentials, result.MessageInfo.MessageText);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task Login_Submit_WithUnknownOrDeletedUser_ShouldReturnSameUnauthorized()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        await fixture.RunAsAsync(id, (IServiceUser service) => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        var deleted = await LoginAsync(login);
        var unknown = await LoginAsync(UniqueLogin());

        Assert.Equal(MessageType.UNAUTHORIZED, deleted.MessageInfo.MessageType);
        Assert.Equal(deleted.MessageInfo.MessageText, unknown.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Login_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsAsync(null, service => service.LoginAsync(null!));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Refresh_Submit_WithActiveToken_ShouldRotateKeepingPersistence()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        var loggedIn = await LoginAsync(login, rememberMe: true);

        var refreshed = await RefreshAsync(loggedIn.Response!.RefreshToken);

        Assert.Equal(MessageType.LOADED, refreshed.MessageInfo.MessageType);
        Assert.NotEqual(loggedIn.Response.RefreshToken, refreshed.Response!.RefreshToken);
        Assert.True(refreshed.Response.IsPersistent);
        Assert.Equal(id, refreshed.Response.User.Id);
        var tokens = await GetTokensAsync(id);
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Equal(tokens[1].Id, tokens[0].ReplacedByTokenId);
        Assert.Null(tokens[1].RevokedAt);
        Assert.True(tokens[1].IsPersistent);
    }

    [Fact]
    public async Task Refresh_Submit_WithRotatedToken_ShouldReturnUnauthorizedAndRevokeAllUserTokens()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        var otherDevice = await LoginAsync(login);
        var loggedIn = await LoginAsync(login);
        var refreshed = await RefreshAsync(loggedIn.Response!.RefreshToken);

        var reused = await RefreshAsync(loggedIn.Response.RefreshToken);

        Assert.Equal(MessageType.UNAUTHORIZED, reused.MessageInfo.MessageType);
        Assert.Contains(AccountTexts.Messages.Validation.RefreshTokenReused, reused.MessageInfo.MessageText);
        Assert.All(await GetTokensAsync(id), token => Assert.NotNull(token.RevokedAt));
        Assert.Equal(MessageType.UNAUTHORIZED, (await RefreshAsync(refreshed.Response!.RefreshToken)).MessageInfo.MessageType);
        Assert.Equal(MessageType.UNAUTHORIZED, (await RefreshAsync(otherDevice.Response!.RefreshToken)).MessageInfo.MessageType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task Refresh_Submit_WithMissingOrUnknownToken_ShouldReturnUnauthorized(string? refreshToken)
    {
        var result = await RefreshAsync(refreshToken);

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Contains(AccountTexts.Messages.Validation.RefreshTokenInvalid, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Refresh_Submit_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        var loggedIn = await LoginAsync(login);
        await fixture.RunAsAsync(null, (DbContextSecurity db) => db.RefreshTokens
            .Where(token => token.UserId == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

        var result = await RefreshAsync(loggedIn.Response!.RefreshToken);

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Contains(AccountTexts.Messages.Validation.RefreshTokenInvalid, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Logout_Submit_WithActiveToken_ShouldRevokeItAndRefreshShouldFail()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        var loggedIn = await LoginAsync(login);

        var result = await RunAsAsync(null, service => service.LogoutAsync(new AccountLogoutRequest { RefreshToken = loggedIn.Response!.RefreshToken }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsLoggedOut);
        Assert.NotNull(Assert.Single(await GetTokensAsync(id)).RevokedAt);
        Assert.Equal(MessageType.UNAUTHORIZED, (await RefreshAsync(loggedIn.Response!.RefreshToken)).MessageInfo.MessageType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown-token")]
    public async Task Logout_Submit_WithoutKnownToken_ShouldStillSucceed(string? refreshToken)
    {
        var result = await RunAsAsync(null, service => service.LogoutAsync(new AccountLogoutRequest { RefreshToken = refreshToken }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Profile_Load_WithCurrentUser_ShouldReturnProfile()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.DisplayProfilePageAsync(new AccountProfilePageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(login, result.Response.Login);
        Assert.Equal("Иван", result.Response.Name);
    }

    [Fact]
    public async Task ProfileLoadAndUpdate_WithoutCurrentUser_ShouldReturnUnauthorized()
    {
        var load = await RunAsAsync(null, service => service.DisplayProfilePageAsync(new AccountProfilePageRequest()));
        var update = await RunAsAsync(null, service => service.UpdateProfileAsync(new AccountProfileUpdateRequest { Name = "Иван" }));

        Assert.Equal(MessageType.UNAUTHORIZED, load.MessageInfo.MessageType);
        Assert.Equal(MessageType.UNAUTHORIZED, update.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Profile_Update_WithPaddedName_ShouldSaveTrimmedNameAndKeepLogin()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.UpdateProfileAsync(new AccountProfileUpdateRequest { Name = "  Иван Петров  " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal("Иван Петров", result.Response!.Name);
        Assert.Equal(login, result.Response.Login);
        var profile = await RunAsAsync(id, service => service.DisplayProfilePageAsync(new AccountProfilePageRequest()));
        Assert.Equal("Иван Петров", profile.Response!.Name);
    }

    [Fact]
    public async Task Profile_Update_WithEmptyName_ShouldReturnInvalid()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var result = await RunAsAsync(id, service => service.UpdateProfileAsync(new AccountProfileUpdateRequest { Name = " " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
    }
}
