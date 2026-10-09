using Microsoft.Playwright;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Временный выбор текущего пользователя внизу боковой панели (task-0010): добавление, выбор, запоминание и заголовок <c>X-User-Id</c>.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class UserPickerE2eTests(E2eAppFixture fixture)
{
    private static ILocator Trigger(IPage page) => page.Locator("app-user-picker .trigger");

    [Fact]
    public async Task AddUser_Submit_WithMixedCaseLogin_ShouldSelectUserKeepAfterReloadAndSendHeader()
    {
        var login = UniqueLogin();
        var name = UniqueName();
        await using var context = await fixture.NewContextAsAsync(null);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await Assertions.Expect(Trigger(page)).ToContainTextAsync("Выберите пользователя");
        await Trigger(page).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "+ Добавить пользователя" }).ClickAsync();
        await Assertions.Expect(page.GetByLabel("Логин нового пользователя")).ToBeFocusedAsync();
        await page.GetByLabel("Логин нового пользователя").FillAsync(login.ToUpperInvariant());
        await page.GetByLabel("Имя нового пользователя").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Создать", Exact = true }).ClickAsync();

        await Assertions.Expect(Trigger(page)).ToContainTextAsync(name);
        var userId = await page.EvaluateAsync<string>("() => localStorage.getItem('socium.currentUserId')");

        var request = await page.RunAndWaitForRequestAsync(
            () => page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded }),
            candidate => candidate.Url.EndsWith("/api/room"));
        Assert.Equal(userId, await request.HeaderValueAsync("x-user-id"));
        await Assertions.Expect(Trigger(page)).ToContainTextAsync(name);

        await Trigger(page).ClickAsync();
        await Assertions.Expect(page.Locator("app-user-picker .user[aria-current=true]")).ToContainTextAsync(login);
    }

    [Fact]
    public async Task User_Click_WithExistingUser_ShouldSelectUserAndClosePanel()
    {
        var name = UniqueName();
        await fixture.Api.CreateUserAsync(UniqueLogin(), name);
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await Trigger(page).ClickAsync();
        await page.Locator("app-user-picker .user", new() { HasText = name }).ClickAsync();

        await Assertions.Expect(Trigger(page)).ToContainTextAsync(name);
        await Assertions.Expect(Trigger(page)).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(page.Locator("app-user-picker .panel")).ToHaveCountAsync(0);
    }
}
