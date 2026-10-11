using Services.Realtime.Models;

namespace Services.Realtime.Api;

public interface IServiceChatNotify
{
    Task NotifyMessageCreatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageUpdatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageDeletedAsync(ChatMessageDeletedNotifyModel deleted, CancellationToken cancellationToken = default);
    Task NotifyParticipantJoinedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default);
    Task NotifyParticipantLeftAsync(ChatParticipantLeftNotifyModel left, CancellationToken cancellationToken = default);
    Task NotifyParticipantRoleChangedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default);
}
