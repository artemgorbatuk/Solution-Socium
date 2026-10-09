using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Окно чата и главная страница через настоящие UI, WebApi и БД.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class ChatPageE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Chat_Click_WithSidebarChat_ShouldOpenKeepAfterReloadAndClose()
    {
        var roomId = await fixture.Api.CreateRoomAsync(UniqueName());
        var chatName = UniqueName();
        var chatId = await fixture.Api.CreateChatAsync(roomId, chatName);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await Assertions.Expect(HomeWordmark(page)).ToBeVisibleAsync();

        var link = page.GetByRole(AriaRole.Link, new() { Name = chatName, Exact = true });
        await link.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/chat/{chatId}$"));
        await Assertions.Expect(link).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(TopbarTitle(page, chatName)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Сообщений пока нет", new() { Exact = true })).ToBeVisibleAsync();
        var message = page.GetByRole(AriaRole.Textbox, new() { Name = $"Сообщение в чат {chatName}", Exact = true });
        var send = page.GetByRole(AriaRole.Button, new() { Name = "Отправить" });
        await Assertions.Expect(send).ToBeDisabledAsync();
        await message.FillAsync("Привет");
        await Assertions.Expect(send).ToBeEnabledAsync();

        await page.ReloadAsync();
        await Assertions.Expect(TopbarTitle(page, chatName)).ToBeVisibleAsync();
        await Assertions.Expect(link).ToHaveAttributeAsync("aria-current", "page");

        await page.GetByRole(AriaRole.Button, new() { Name = "Закрыть чат" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/$"));
        await Assertions.Expect(HomeWordmark(page)).ToBeVisibleAsync();
        await Assertions.Expect(link).Not.ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task Chat_Delete_WithOpenChat_ShouldReturnHome()
    {
        var roomId = await fixture.Api.CreateRoomAsync(UniqueName());
        var chatName = UniqueName();
        var chatId = await fixture.Api.CreateChatAsync(roomId, chatName);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await Assertions.Expect(TopbarTitle(page, chatName)).ToBeVisibleAsync();

        await Assertions.Expect(page.RowAction($"Удалить чат {chatName}").Locator("..")).ToHaveCSSAsync("opacity", "1");
        await page.ClickRowActionAsync($"Удалить чат {chatName}");
        var confirm = page.GetByRole(AriaRole.Group, new() { Name = "Удалить чат?" });
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/$"));
        await Assertions.Expect(HomeWordmark(page)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Heading)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Chat_Load_WithUnknownId_ShouldShowNotFoundAndLinkHome()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{Guid.NewGuid()}");

        await Assertions.Expect(page.GetByText("Чат не найден", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "На главную" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/$"));
        await Assertions.Expect(HomeWordmark(page)).ToBeVisibleAsync();
    }

    private static ILocator HomeWordmark(IPage page) =>
        page.GetByRole(AriaRole.Heading, new() { Name = "Socium", Exact = true, Level = 1 });

    private static ILocator TopbarTitle(IPage page, string chatName) =>
        page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Heading, new() { Name = chatName, Exact = true });
}
