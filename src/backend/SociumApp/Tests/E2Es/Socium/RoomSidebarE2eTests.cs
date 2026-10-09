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
        await page.ClickRowActionAsync($"Переименовать комнату {name}");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Новое название комнаты" }).FillAsync(newName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить" }).ClickAsync();
        await Assertions.Expect(rooms.GetByText(newName, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(rooms.GetByText(name, new() { Exact = true })).ToHaveCountAsync(0);

        await page.ClickRowActionAsync($"Удалить комнату {newName}");
        var confirm = rooms.GetByRole(AriaRole.Group, new() { Name = "Удалить комнату?" });
        await Assertions.Expect(confirm).ToContainTextAsync($"Удалить комнату «{newName}»?");
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Удалить" }).ClickAsync();
        await Assertions.Expect(confirm).ToHaveCountAsync(0);
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

        await page.ClickRowActionAsync($"Переименовать комнату {name}");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Новое название комнаты" }).FillAsync(existingName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Сохранить" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync(RoomCrudTexts.Messages.Validation.NameAlreadyExists);
    }

    [Fact]
    public async Task Room_CancelDelete_WithEscape_ShouldKeepRoom()
    {
        var name = UniqueName();
        await fixture.Api.CreateRoomAsync(name);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.ClickRowActionAsync($"Удалить комнату {name}");
        var rooms = page.GetByRole(AriaRole.Navigation, new() { Name = "Комнаты" });
        var confirm = rooms.GetByRole(AriaRole.Group, new() { Name = "Удалить комнату?" });
        await Assertions.Expect(confirm).ToContainTextAsync(name);
        var cancel = confirm.GetByRole(AriaRole.Button, new() { Name = "Отмена" });
        await Assertions.Expect(cancel).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(confirm).ToHaveCountAsync(0);
        await Assertions.Expect(rooms.GetByText(name, new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Room_Hover_WithExistingRoom_ShouldRevealActionButtons()
    {
        var name = UniqueName();
        await fixture.Api.CreateRoomAsync(name);

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        var actions = page.RowAction($"Удалить комнату {name}").Locator("..");
        await Assertions.Expect(actions).ToHaveCSSAsync("opacity", "0");

        var rooms = page.GetByRole(AriaRole.Navigation, new() { Name = "Комнаты" });
        await rooms.GetByText(name, new() { Exact = true }).HoverAsync();

        await Assertions.Expect(actions).ToHaveCSSAsync("opacity", "1");
        await Assertions.Expect(page.RowAction($"Новый чат в комнате {name}")).ToBeVisibleAsync();
        await Assertions.Expect(page.RowAction($"Переименовать комнату {name}")).ToBeVisibleAsync();
    }
}
