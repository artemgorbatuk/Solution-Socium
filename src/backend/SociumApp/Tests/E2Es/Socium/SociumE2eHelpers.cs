using System.Net;
using System.Net.Http.Json;
using Microsoft.Playwright;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace Tests.E2Es.Socium;

/// <summary>
/// Общие шаги E2E area Socium: подготовка и проверка данных через HTTP-клиент фикстуры, действия в боковой панели.
/// </summary>
internal static class SociumE2eHelpers
{
    public static string UniqueName() => $"E2E {Guid.NewGuid():N}";

    public static async Task<Guid> CreateRoomAsync(this HttpClient api, string name)
    {
        var response = await api.PostAsJsonAsync("/api/room", new RoomCreateRequest { Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await api.GetFromJsonAsync<ApiSuccessResponse<RoomListPageResponse>>("/api/room", TestContext.Current.CancellationToken);
        return list!.Response!.Rows.Single(row => row.Name == name).Id;
    }

    /// <summary>Создаёт чат; название должно быть уникальным в комнате, чтобы вернуть его Id.</summary>
    public static async Task<Guid> CreateChatAsync(this HttpClient api, Guid roomId, string name)
    {
        var request = new ChatCreateRequest { RoomId = roomId, Name = name };
        var response = await api.PostAsJsonAsync("/api/chat", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await api.GetChatListAsync(roomId);
        return list.Rows.Single(row => row.Name == name).Id;
    }

    public static async Task<ChatListPageResponse> GetChatListAsync(this HttpClient api, Guid roomId)
    {
        var body = await api.GetFromJsonAsync<ApiSuccessResponse<ChatListPageResponse>>($"/api/chat?roomId={roomId}", TestContext.Current.CancellationToken);
        return body!.Response!;
    }

    /// <summary>Создаёт сообщение; текст должен быть уникальным в чате, чтобы вернуть его Id.</summary>
    public static async Task<Guid> CreateMessageAsync(this HttpClient api, Guid chatId, string text)
    {
        var request = new MessageCreateRequest { ChatId = chatId, Text = text };
        var response = await api.PostAsJsonAsync("/api/message", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await api.GetMessageListAsync(chatId);
        return list.Rows.Single(row => row.Text == text).Id;
    }

    public static async Task<MessageListPageResponse> GetMessageListAsync(this HttpClient api, Guid chatId)
    {
        var body = await api.GetFromJsonAsync<ApiSuccessResponse<MessageListPageResponse>>($"/api/message?chatId={chatId}", TestContext.Current.CancellationToken);
        return body!.Response!;
    }

    public static Task OpenRoomMenuAsync(this IPage page, string roomName) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Действия с комнатой {roomName}", Exact = true }).ClickAsync();
}
