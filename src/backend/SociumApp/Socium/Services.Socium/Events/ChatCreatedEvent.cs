using Services.Shared.Enums;
using Services.Shared.Events;
using Services.Socium.Api;
using Services.Socium.Mapping;

namespace Services.Socium.Events;

/// <summary>Чат добавлен в UoW; Id уже назначен генератором.</summary>
public sealed class ChatCreatedEvent : IDomainEvent
{
    public required Guid ChatId { get; init; }
    public required Guid CreatorUserId { get; init; }
}

/// <summary>Готовит участника-создателя через <see cref="IServiceParticipant.CreateStageAsync"/> (с <c>ChatAdmin</c>).</summary>
public sealed class CreateCreatorParticipantOnChatCreatedHandler : IDomainEventHandler<ChatCreatedEvent>
{
    private readonly IServiceParticipant serviceParticipant;

    public CreateCreatorParticipantOnChatCreatedHandler(IServiceParticipant serviceParticipant)
    {
        this.serviceParticipant = serviceParticipant;
    }

    public async Task HandleAsync(ChatCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var stageRequest = ParticipantMapper.ToCreateStageRequest(
            domainEvent.ChatId,
            domainEvent.CreatorUserId,
            assignChatAdmin: true);
        var result = await serviceParticipant.CreateStageAsync(stageRequest, cancellationToken);

        if (result.MessageInfo.MessageType != MessageType.SAVED)
        {
            throw new InvalidOperationException(result.MessageInfo.MessageText);
        }
    }
}
