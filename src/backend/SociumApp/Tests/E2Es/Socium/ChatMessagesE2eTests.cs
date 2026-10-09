using Microsoft.Playwright;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Сообщения в окне чата через настоящие UI, WebApi и БД.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class ChatMessagesE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Message_Send_WithEnterAfterShiftEnter_ShouldShowMultilineMessageAndKeepAfterReload()
    {
        var (chatId, chatName) = await CreateChatAsync();

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        var composer = page.GetByRole(AriaRole.Textbox, new() { Name = $"Сообщение в чат {chatName}", Exact = true });
        await composer.PressSequentiallyAsync("Строка 1");
        await composer.PressAsync("Shift+Enter");
        await composer.PressSequentiallyAsync("Строка 2");
        await composer.PressAsync("Enter");

        var item = Messages(page, chatName).GetByRole(AriaRole.Listitem);
        await Assertions.Expect(item).ToHaveCountAsync(1);
        await Assertions.Expect(item).ToContainTextAsync("Строка 2");
        await Assertions.Expect(composer).ToHaveValueAsync(string.Empty);
        var row = Assert.Single((await fixture.Api.GetMessageListAsync(chatId)).Rows);
        Assert.Equal("Строка 1\nСтрока 2", row.Text);

        await page.ReloadAsync();
        await Assertions.Expect(item).ToContainTextAsync("Строка 1");
    }

    [Fact]
    public async Task Message_Edit_WithMenuAndEnter_ShouldChangeText()
    {
        var (chatId, chatName) = await CreateChatAsync();
        await fixture.Api.CreateMessageAsync(chatId, "Исходный текст");

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        var item = Messages(page, chatName).GetByRole(AriaRole.Listitem);
        await item.HoverAsync();
        await item.GetByRole(AriaRole.Button, new() { Name = "Действия с сообщением" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Изменить" }).ClickAsync();
        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Новый текст сообщения" });
        await Assertions.Expect(field).ToHaveValueAsync("Исходный текст");
        await field.FillAsync("Изменённый текст");
        await field.PressAsync("Enter");

        await Assertions.Expect(field).ToHaveCountAsync(0);
        await Assertions.Expect(item).ToContainTextAsync("Изменённый текст");
        var row = Assert.Single((await fixture.Api.GetMessageListAsync(chatId)).Rows);
        Assert.Equal("Изменённый текст", row.Text);
    }

    [Fact]
    public async Task Message_Delete_WithConfirm_ShouldRemoveMessage()
    {
        var (chatId, chatName) = await CreateChatAsync();
        await fixture.Api.CreateMessageAsync(chatId, "Удаляемое сообщение");

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        var item = Messages(page, chatName).GetByRole(AriaRole.Listitem);
        await item.HoverAsync();
        await item.GetByRole(AriaRole.Button, new() { Name = "Действия с сообщением" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Удалить" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Удалить сообщение?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Удаляемое сообщение");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();

        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("Сообщений пока нет", new() { Exact = true })).ToBeVisibleAsync();
        Assert.Empty((await fixture.Api.GetMessageListAsync(chatId)).Rows);
    }

    private async Task<(Guid ChatId, string ChatName)> CreateChatAsync()
    {
        var roomId = await fixture.Api.CreateRoomAsync(UniqueName());
        var chatName = UniqueName();
        return (await fixture.Api.CreateChatAsync(roomId, chatName), chatName);
    }

    private static ILocator Messages(IPage page, string chatName) =>
        page.GetByRole(AriaRole.List, new() { Name = $"Сообщения чата {chatName}", Exact = true });
}
