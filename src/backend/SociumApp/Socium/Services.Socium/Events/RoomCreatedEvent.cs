using Services.Shared.Enums;
using Services.Shared.Events;
using Services.Socium.Api;
using Services.Socium.Mapping;
using Services.Socium.Texts;

namespace Services.Socium.Events;

/// <summary>Комната добавлена в UoW; Id уже назначен генератором.</summary>
public sealed class RoomCreatedEvent : IDomainEvent
{
    public required Guid RoomId { get; init; }
    public required Guid CreatorUserId { get; init; }
}

/// <summary>Готовит чат по умолчанию через <see cref="IServiceChat.CreateStageAsync"/>.</summary>
public sealed class CreateDefaultChatOnRoomCreatedHandler : IDomainEventHandler<RoomCreatedEvent>
{
    private readonly IServiceChat serviceChat;

    public CreateDefaultChatOnRoomCreatedHandler(IServiceChat serviceChat)
    {
        this.serviceChat = serviceChat;
    }

    public async Task HandleAsync(RoomCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var stageRequest = ChatMapper.ToCreateStageRequest(
            domainEvent.RoomId,
            domainEvent.CreatorUserId,
            ChatCrudTexts.Defaults.Name);
        var result = await serviceChat.CreateStageAsync(stageRequest, cancellationToken);

        if (result.MessageInfo.MessageType != MessageType.SAVED)
        {
            throw new InvalidOperationException(result.MessageInfo.MessageText);
        }
    }
}
