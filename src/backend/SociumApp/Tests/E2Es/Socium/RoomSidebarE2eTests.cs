using Microsoft.Playwright;
using Services.Socium.Texts;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Комнаты в боковой панели через настоящие UI, WebApi и БД.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class RoomSidebarE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Room_CreateRenameDelete_WithUniqueNames_ShouldUpdateListAndApi()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        var rooms = page.GetByRole(AriaRole.Navigation, new() { Name = "Комнаты" });

        var name = UniqueName();
        await page.GetByRole(AriaRole.Button, new() { Name = "+ Новая комната" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Название новой комнаты" }).FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Создать", Exact = true }).ClickAsync();
        await Assertions.Expect(rooms.GetByText(name, new() { Exact = true })).ToBeVisibleAsync();

        var newName = UniqueName();
        await page.OpenRoomMenuAsync(name);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Переименовать" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Новое название комнаты" }).FillAsync(newName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить" }).ClickAsync();
        await Assertions.Expect(rooms.GetByText(newName, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(rooms.GetByText(name, new() { Exact = true })).ToHaveCountAsync(0);

        await page.OpenRoomMenuAsync(newName);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Удалить" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Удалить комнату?" });
        await Assertions.Expect(dialog).ToContainTextAsync(newName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(rooms.GetByText(newName, new() { Exact = true })).ToHaveCountAsync(0);

        var list = await fixture.Api.GetStringAsync("/api/room", TestContext.Current.CancellationToken);
        Assert.DoesNotContain(name, list);
        Assert.DoesNotContain(newName, list);
    }

    [Fact]
    public async Task Room_Rename_WithDuplicateName_ShouldShowServerError()
    {
        var existingName = UniqueName();
        var name = UniqueName();
        await fixture.Api.CreateRoomAsync(existingName);
        await fixture.Api.CreateRoomAsync(name);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.OpenRoomMenuAsync(name);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Переименовать" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Новое название комнаты" }).FillAsync(existingName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync(RoomCrudTexts.Messages.Validation.NameAlreadyExists);
    }

    [Fact]
    public async Task Room_CancelDelete_WithExistingRoom_ShouldKeepRoom()
    {
        var name = UniqueName();
        await fixture.Api.CreateRoomAsync(name);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.OpenRoomMenuAsync(name);
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Удалить" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Удалить комнату?" });
        await Assertions.Expect(dialog).ToContainTextAsync(name);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Отмена" }).ClickAsync();

        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        var rooms = page.GetByRole(AriaRole.Navigation, new() { Name = "Комнаты" });
        await Assertions.Expect(rooms.GetByText(name, new() { Exact = true })).ToBeVisibleAsync();
    }
}
