using System.Net;
using Microsoft.Playwright;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Чаты в дереве боковой панели через настоящие UI, WebApi и БД.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class ChatSidebarE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Room_Toggle_WithExpandedRoom_ShouldHideChatsAndKeepCollapsedAfterReload()
    {
        var roomName = UniqueName();
        var roomId = await fixture.Api.CreateRoomAsync(roomName);
        await fixture.Api.CreateChatAsync(roomId, "Общий");
        await fixture.Api.CreateChatAsync(roomId, "Арт");

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        var toggle = page.GetByRole(AriaRole.Button, new() { Name = $"Чаты комнаты {roomName}", Exact = true });
        var chats = ChatList(page, roomName);
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        string[] expectedNames = ["Арт", "Общий"];
        await Assertions.Expect(chats.GetByRole(AriaRole.Listitem)).ToContainTextAsync(expectedNames);

        await toggle.ClickAsync();
        await Assertions.Expect(chats).ToHaveCountAsync(0);

        await page.ReloadAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(chats).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Chat_CreateRenameDelete_WithUniqueNames_ShouldUpdateTreeAndApi()
    {
        var roomName = UniqueName();
        var roomId = await fixture.Api.CreateRoomAsync(roomName);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        var chats = ChatList(page, roomName);

        var name = UniqueName();
        await page.OpenRoomMenuAsync(roomName);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Новый чат" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = $"Название нового чата в комнате {roomName}" }).FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Создать", Exact = true }).ClickAsync();
        await Assertions.Expect(chats.GetByText(name, new() { Exact = true })).ToBeVisibleAsync();

        var newName = UniqueName();
        await page.GetByRole(AriaRole.Button, new() { Name = $"Действия с чатом {name}", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Переименовать" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Новое название чата" }).FillAsync(newName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить" }).ClickAsync();
        await Assertions.Expect(chats.GetByText(newName, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(chats.GetByText(name, new() { Exact = true })).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Действия с чатом {newName}", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Удалить" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Удалить чат?" });
        await Assertions.Expect(dialog).ToContainTextAsync(newName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(chats).ToContainTextAsync("Чатов пока нет");

        var list = await fixture.Api.GetChatListAsync(roomId);
        Assert.Empty(list.Rows);
    }

    [Fact]
    public async Task Room_Delete_WithChats_ShouldWarnAboutChatCountAndDeleteChats()
    {
        var roomName = UniqueName();
        var roomId = await fixture.Api.CreateRoomAsync(roomName);
        await fixture.Api.CreateChatAsync(roomId, "Общий");
        await fixture.Api.CreateChatAsync(roomId, "Арт");

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.OpenRoomMenuAsync(roomName);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Удалить" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Удалить комнату?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Вместе с комнатой будут удалены чаты: 2");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);

        var response = await fixture.Api.GetAsync($"/api/chat?roomId={roomId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static ILocator ChatList(IPage page, string roomName) =>
        page.GetByRole(AriaRole.List, new() { Name = $"Чаты комнаты {roomName}", Exact = true });
}
