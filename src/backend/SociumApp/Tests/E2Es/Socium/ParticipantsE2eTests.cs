using Microsoft.Playwright;
using static Tests.E2Es.Socium.SociumE2eHelpers;

namespace Tests.E2Es.Socium;

/// <summary>
/// Участники чата (task-0010): вступление, свои и чужие сообщения, роль админа и выход через настоящие UI, WebApi и БД.
/// </summary>
[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class ParticipantsE2eTests(E2eAppFixture fixture)
{
    [Fact]
    public async Task Join_Click_WithNotParticipant_ShouldShowComposerWithoutAdminActions()
    {
        var (chatId, chatName) = await CreateChatAsync();
        var userId = await fixture.Api.CreateUserAsync(UniqueLogin(), UniqueName());

        await using var context = await fixture.NewContextAsAsync(userId);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await Assertions.Expect(page.GetByText("Вы не участник этого чата")).ToBeVisibleAsync();
        await Assertions.Expect(Composer(page, chatName)).ToHaveCountAsync(0);
        await Assertions.Expect(ParticipantsButton(page)).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Вступить в чат" }).ClickAsync();

        await Assertions.Expect(Composer(page, chatName)).ToBeVisibleAsync();
        await Assertions.Expect(ParticipantsButton(page)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = chatName, Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.RowAction($"Переименовать чат {chatName}")).ToHaveCountAsync(0);
        var participant = Assert.Single((await fixture.Api.GetParticipantListAsync(chatId)).Rows, row => row.UserId == userId);
        Assert.False(participant.IsAdmin);
    }

    [Fact]
    public async Task Messages_Render_WithOtherSender_ShouldShowOwnRightAndOtherLeftWithNames()
    {
        var (chatId, chatName) = await CreateChatAsync();
        var otherName = UniqueName();
        using var other = fixture.CreateApiAs(await fixture.Api.CreateUserAsync(UniqueLogin(), otherName));
        await other.JoinChatAsync(chatId);
        await other.CreateMessageAsync(chatId, "Чужое сообщение");
        await fixture.Api.CreateMessageAsync(chatId, "Своё сообщение");

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        var items = Messages(page, chatName).GetByRole(AriaRole.Listitem);
        await Assertions.Expect(items).ToHaveCountAsync(2);
        var otherItem = items.Filter(new() { HasText = "Чужое сообщение" });
        var ownItem = items.Filter(new() { HasText = "Своё сообщение" });

        await Assertions.Expect(otherItem).ToContainTextAsync(otherName);
        await Assertions.Expect(otherItem.Locator(".row.own")).ToHaveCountAsync(0);
        await otherItem.HoverAsync();
        await Assertions.Expect(otherItem.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
        await Assertions.Expect(ownItem.Locator(".row.own")).ToHaveCountAsync(1);
        await Assertions.Expect(ownItem.Locator(".sender")).ToHaveTextAsync(E2eAppFixture.DefaultUserName);
        await ownItem.HoverAsync();
        await Assertions.Expect(ownItem.GetByRole(AriaRole.Button, new() { Name = "Изменить сообщение" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Role_Grant_WithAdmin_ShouldMarkParticipantAsAdmin()
    {
        var (chatId, chatName) = await CreateChatAsync();
        var otherName = UniqueName();
        var otherId = await fixture.Api.CreateUserAsync(UniqueLogin(), otherName);
        using (var other = fixture.CreateApiAs(otherId))
        {
            await other.JoinChatAsync(chatId);
        }

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await ParticipantsButton(page).ClickAsync();
        var otherItem = Participants(page, chatName).GetByRole(AriaRole.Listitem).Filter(new() { HasText = otherName });
        await Assertions.Expect(otherItem.Locator(".badge")).ToHaveCountAsync(0);

        await otherItem.GetByRole(AriaRole.Button, new() { Name = $"Сделать админом: {otherName}" }).ClickAsync();

        await Assertions.Expect(otherItem.Locator(".badge")).ToHaveTextAsync("админ");
        await Assertions.Expect(otherItem.GetByRole(AriaRole.Button, new() { Name = $"Снять роль админа: {otherName}" })).ToBeVisibleAsync();
        var participant = Assert.Single((await fixture.Api.GetParticipantListAsync(chatId)).Rows, row => row.UserId == otherId);
        Assert.True(participant.IsAdmin);
    }

    [Fact]
    public async Task Panel_Reload_WithOpenPanel_ShouldStayOpenUntilClosed()
    {
        var (chatId, chatName) = await CreateChatAsync();

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await ParticipantsButton(page).ClickAsync();
        await Assertions.Expect(Participants(page, chatName)).ToBeVisibleAsync();

        await page.ReloadAsync();
        await Assertions.Expect(Participants(page, chatName)).ToBeVisibleAsync();
        await Assertions.Expect(ParticipantsButton(page)).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Закрыть панель участников" }).ClickAsync();
        await page.ReloadAsync();
        await Assertions.Expect(Composer(page, chatName)).ToBeVisibleAsync();
        await Assertions.Expect(Participants(page, chatName)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Leave_Click_WithLastAdmin_ShouldShowReasonAndStayParticipant()
    {
        var (chatId, chatName) = await CreateChatAsync();
        using (var other = fixture.CreateApiAs(await fixture.Api.CreateUserAsync(UniqueLogin(), UniqueName())))
        {
            await other.JoinChatAsync(chatId);
        }

        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await ParticipantsButton(page).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Покинуть чат" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Вы последний админ чата — сначала назначьте админом другого участника.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Покинуть", Exact = true })).ToHaveCountAsync(0);
        Assert.Equal(2, (await fixture.Api.GetParticipantListAsync(chatId)).RowCount);
        await Assertions.Expect(Composer(page, chatName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Leave_Confirm_WithOrdinaryParticipant_ShouldShowJoinStubAndKeepMessages()
    {
        var (chatId, chatName) = await CreateChatAsync();
        var userId = await fixture.Api.CreateUserAsync(UniqueLogin(), UniqueName());
        using (var user = fixture.CreateApiAs(userId))
        {
            await user.JoinChatAsync(chatId);
            await user.CreateMessageAsync(chatId, "Сообщение ушедшего");
        }

        await using var context = await fixture.NewContextAsAsync(userId);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");
        await ParticipantsButton(page).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Покинуть чат" }).ClickAsync();
        await Assertions.Expect(page.GetByText($"Покинуть чат «{chatName}»? Ваши сообщения останутся в чате.")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Покинуть", Exact = true }).ClickAsync();

        await Assertions.Expect(page.GetByText("Вы не участник этого чата")).ToBeVisibleAsync();
        await Assertions.Expect(ParticipantsButton(page)).ToHaveCountAsync(0);
        Assert.DoesNotContain((await fixture.Api.GetParticipantListAsync(chatId)).Rows, row => row.UserId == userId);
        Assert.Contains((await fixture.Api.GetMessageListAsync(chatId)).Rows, row => row.Text == "Сообщение ушедшего");
    }

    [Fact]
    public async Task Chat_Open_WithoutCurrentUser_ShouldAskToChooseUserWithoutJoinButton()
    {
        var (chatId, chatName) = await CreateChatAsync();

        await using var context = await fixture.NewContextAsAsync(null);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/chat/{chatId}");

        await Assertions.Expect(page.GetByText("Выберите пользователя внизу боковой панели, чтобы читать чат и писать в него")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Вступить в чат" })).ToHaveCountAsync(0);
        await Assertions.Expect(Composer(page, chatName)).ToHaveCountAsync(0);
    }

    private async Task<(Guid ChatId, string ChatName)> CreateChatAsync()
    {
        var roomId = await fixture.Api.CreateRoomAsync(UniqueName());
        var chatName = UniqueName();
        return (await fixture.Api.CreateChatAsync(roomId, chatName), chatName);
    }

    private static ILocator Composer(IPage page, string chatName) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = $"Сообщение в чат {chatName}", Exact = true });

    private static ILocator Messages(IPage page, string chatName) =>
        page.GetByRole(AriaRole.List, new() { Name = $"Сообщения чата {chatName}", Exact = true });

    private static ILocator Participants(IPage page, string chatName) =>
        page.GetByRole(AriaRole.List, new() { Name = $"Участники чата {chatName}", Exact = true });

    private static ILocator ParticipantsButton(IPage page) =>
        page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button, new() { Name = "Участники", Exact = true });
}
