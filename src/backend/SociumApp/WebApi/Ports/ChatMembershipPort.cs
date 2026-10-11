using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
using Services.Realtime.Api;

namespace WebApi.Ports;

/// <summary>
/// Проверка участника чата для Join на ChatHub.
/// </summary>
public class ChatMembershipPort : IChatMembership
{
    private readonly IUnitOfWorkSocium unitOfWork;

    public ChatMembershipPort(IUnitOfWorkSocium unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> IsParticipantAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        var participants = await unitOfWork.Participants.GetListAsync(
            new ParticipantQueryOptions { ChatId = chatId, UserId = userId },
            cancellationToken);
        return participants.Any();
    }
}
