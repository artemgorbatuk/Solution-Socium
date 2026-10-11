using Microsoft.Playwright;
using Tests.E2Es.Socium;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Security;

/// <summary>
/// Вход, регистрация, профиль и выход (task-0011) через настоящие UI, WebApi и БД: refresh-cookie ставит и отправляет сам браузер.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class AccountE2eTests(E2eAppFixture fixture)
{
    private const string RefreshCookieName = "socium_refresh";

    private static ILocator Form(IPage page, string name) => page.GetByRole(AriaRole.Form, new() { Name = name, Exact = true });

    private static ILocator AccountTrigger(IPage page) => page.Locator("app-account-menu .trigger");

    private static async Task FillLoginAsync(IPage page, string login, string password, bool rememberMe = false)
    {
        await page.GetByLabel("Логин").FillAsync(login);
        await page.GetByLabel("Пароль").FillAsync(password);
        if (rememberMe)
        {
            await page.GetByLabel("Запомнить меня").CheckAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Войти", Exact = true }).ClickAsync();
    }

    [Fact]
    public async Task Chat_Open_WithoutSession_ShouldShowLoginAndReturnToChatAfterLogin()
    {
        var roomId = await fixture.Api.CreateRoomAsync(UniqueName());
        var chatName = UniqueName();
        var chatId = await fixture.Api.CreateChatAsync(roomId, chatName);
        var login = UniqueLogin();
        var name = UniqueName();
        await fixture.CreateUserAsync(login, name);
        await using var context = await fixture.NewContextAsAsync(null);
        var page = await context.NewPageAsync();

        await page.GotoAsync($"/chat/{chatId}");
        await Assertions.Expect(Form(page, "Вход")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("app-sidebar")).ToHaveCountAsync(0);
        Assert.Contains("returnUrl=", page.Url);

        await FillLoginAsync(page, login.ToUpperInvariant(), E2eAppFixture.Password);

        await page.WaitForURLAsync($"**/chat/{chatId}");
        await Assertions.Expect(page.GetByText("Вы не участник этого чата")).ToBeVisibleAsync();
        await Assertions.Expect(AccountTrigger(page)).ToContainTextAsync(name);
    }

    [Fact]
    public async Task Login_Submit_WithWrongPassword_ShouldShowGeneralErrorAndStayOnLogin()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, UniqueName());
        await using var context = await fixture.NewContextAsAsync(null);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await FillLoginAsync(page, login, "wrong-password");

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Неверный логин или пароль.");
        await Assertions.Expect(Form(page, "Вход")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Register_Submit_WithNewUser_ShouldOpenCheckEmailThenAllowLoginAfterConfirm()
    {
        var login = UniqueLogin();
        var email = $"{login}@example.com";
        var name = UniqueName();
        await using var context = await fixture.NewContextAsAsync(null);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.GetByRole(AriaRole.Button, new() { Name = "Регистрация", Exact = true }).ClickAsync();
        await Assertions.Expect(Form(page, "Регистрация")).ToBeVisibleAsync();
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Логин").FillAsync(login);
        await page.GetByLabel("Имя").FillAsync(name);
        await page.GetByLabel("Пароль").FillAsync(E2eAppFixture.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Зарегистрироваться", Exact = true }).ClickAsync();

        await Assertions.Expect(Form(page, "Проверьте почту")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByLabel("Email")).ToHaveValueAsync(email);
        await Assertions.Expect(AccountTrigger(page)).ToHaveCountAsync(0);

        await fixture.ConfirmEmailForLoginAsync(login);
        await page.GetByRole(AriaRole.Link, new() { Name = "Войти", Exact = true }).ClickAsync();
        await FillLoginAsync(page, login, E2eAppFixture.Password);

        await Assertions.Expect(AccountTrigger(page)).ToContainTextAsync(name);
        await AccountTrigger(page).ClickAsync();
        await Assertions.Expect(page.Locator("app-account-menu .email")).ToContainTextAsync(email);
    }

    [Fact]
    public async Task Login_Submit_WithRememberMe_ShouldSetPersistentHttpOnlyCookieOtherwiseSessionCookie()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, UniqueName());
        await using var remembered = await fixture.NewContextAsAsync(null);
        await using var session = await fixture.NewContextAsAsync(null);
        var rememberedPage = await remembered.NewPageAsync();
        var sessionPage = await session.NewPageAsync();
        await rememberedPage.GotoAsync("/");
        await sessionPage.GotoAsync("/");

        await FillLoginAsync(rememberedPage, login, E2eAppFixture.Password, rememberMe: true);
        await FillLoginAsync(sessionPage, login, E2eAppFixture.Password);
        await Assertions.Expect(AccountTrigger(rememberedPage)).ToBeVisibleAsync();
        await Assertions.Expect(AccountTrigger(sessionPage)).ToBeVisibleAsync();

        var rememberedCookie = Assert.Single(await remembered.CookiesAsync(), cookie => cookie.Name == RefreshCookieName);
        var sessionCookie = Assert.Single(await session.CookiesAsync(), cookie => cookie.Name == RefreshCookieName);
        Assert.True(rememberedCookie.HttpOnly);
        Assert.Equal("/api/account", rememberedCookie.Path);
        Assert.Equal(SameSiteAttribute.Strict, rememberedCookie.SameSite);
        Assert.InRange(DateTimeOffset.FromUnixTimeSeconds((long)rememberedCookie.Expires).UtcDateTime, DateTime.UtcNow.AddDays(13), DateTime.UtcNow.AddDays(15));
        Assert.Equal(-1, sessionCookie.Expires);
    }

    [Fact]
    public async Task Profile_EditName_WithNewName_ShouldShowNewNameAfterReload()
    {
        var newName = UniqueName();
        var userId = await fixture.CreateUserAsync(UniqueLogin(), UniqueName());
        await using var context = await fixture.NewContextAsAsync(userId);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await AccountTrigger(page).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Изменить имя", Exact = true }).ClickAsync();
        await page.GetByLabel("Новое имя").FillAsync(newName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить", Exact = true }).ClickAsync();

        await Assertions.Expect(AccountTrigger(page)).ToContainTextAsync(newName);
        await page.ReloadAsync();
        await Assertions.Expect(AccountTrigger(page)).ToContainTextAsync(newName);
    }

    [Fact]
    public async Task Logout_Click_WithSession_ShouldOpenLoginAndNotRestoreSessionAfterReload()
    {
        var userId = await fixture.CreateUserAsync(UniqueLogin(), UniqueName());
        await using var context = await fixture.NewContextAsAsync(userId);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await AccountTrigger(page).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Выйти", Exact = true }).ClickAsync();

        await Assertions.Expect(Form(page, "Вход")).ToBeVisibleAsync();
        Assert.DoesNotContain(await context.CookiesAsync(), cookie => cookie.Name == RefreshCookieName);
        await page.ReloadAsync();
        await Assertions.Expect(Form(page, "Вход")).ToBeVisibleAsync();
    }
}
