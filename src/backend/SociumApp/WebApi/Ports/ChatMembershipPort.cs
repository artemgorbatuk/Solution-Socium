using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
using Services.Realtime.Api;
using Services.Socium.Ports;

namespace WebApi.Ports;

/// <summary>
/// Проверка участника чата для Join / typing на ChatHub.
/// </summary>
public class ChatMembershipPort : IChatMembership
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly IUserPort userPort;

    public ChatMembershipPort(IUnitOfWorkSocium unitOfWork, IUserPort userPort)
    {
        this.unitOfWork = unitOfWork;
        this.userPort = userPort;
    }

    public async Task<bool> IsParticipantAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        var participants = await unitOfWork.Participants.GetListAsync(
            new ParticipantQueryOptions { ChatId = chatId, UserId = userId },
            cancellationToken);
        return participants.Any();
    }

    public async Task<string?> GetParticipantNameAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(chatId, userId, cancellationToken))
        {
            return null;
        }

        var user = await userPort.GetSingleOrDefaultAsync(userId, cancellationToken);
        return user?.Name;
    }
}
