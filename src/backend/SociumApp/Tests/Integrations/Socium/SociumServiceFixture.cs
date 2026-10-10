using Services.Socium.Api;
using Services.Socium.Models;
using Tests.Infrastructure;

namespace Tests.Integrations.Socium;

/// <summary>
/// Сервисы Socium поверх временной БД; пользователи создаются через сервис Security, как при регистрации.
/// </summary>
public sealed class SociumServiceFixture : ServiceFixtureBase
{
    public SociumServiceFixture() : base("socium_service") { }

    public async Task<Guid> CreateRoomAsync(string name)
    {
        var created = await RunAsync((IServiceRoom service) => service.CreateAsync(new RoomCreateRequest { Name = name }));
        EnsureSaved(created, $"комнату «{name}»");

        var list = await RunAsync((IServiceRoom service) => service.DisplayListPageAsync(new RoomListPageRequest()));
        return list.Response!.Rows.Single(row => row.Name == name.Trim()).Id;
    }

    public async Task<Guid> CreateChatAsync(Guid roomId, string name)
    {
        var created = await RunAsync((IServiceChat service) => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = name }));
        EnsureSaved(created, $"чат «{name}»");

        var list = await RunAsync((IServiceChat service) => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));
        return list.Response!.Rows.Single(row => row.Name == name.Trim()).Id;
    }

    /// <summary>Создаёт сообщение; текст должен быть уникальным в чате, чтобы вернуть его Id.</summary>
    public async Task<Guid> CreateMessageAsync(Guid chatId, string text)
    {
        var created = await RunAsync((IServiceMessage service) => service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = text }));
        EnsureSaved(created, $"сообщение «{text}»");

        var list = await RunAsync((IServiceMessage service) => service.DisplayListPageAsync(new MessageListPageRequest { ChatId = chatId }));
        return list.Response!.Rows.Single(row => row.Text == text.Trim()).Id;
    }

    public async Task JoinChatAsync(Guid chatId, Guid userId)
    {
        var created = await RunAsAsync(userId, (IServiceParticipant service) => service.CreateAsync(new ParticipantCreateRequest { ChatId = chatId }));
        EnsureSaved(created, $"участника чата {chatId}");
    }
}

[CollectionDefinition(Name)]
public sealed class SociumServiceCollection : ICollectionFixture<SociumServiceFixture>
{
    public const string Name = "Socium.Services";
}
