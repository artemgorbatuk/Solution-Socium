using Datasource.Files.Ef.Models;
using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
using Services.Files.Stored.Ports;

namespace WebApi.Ports;

/// <summary>
/// Доступ к файлу сообщения: пользователь — участник чата этого сообщения.
/// </summary>
public class MessageFileOwnerAccess : IFileOwnerAccess
{
    private readonly IUnitOfWorkSocium unitOfWork;

    public MessageFileOwnerAccess(IUnitOfWorkSocium unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> CanReadAsync(FileOwnerType ownerType, Guid ownerId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (ownerType != FileOwnerType.Message)
        {
            return false;
        }

        var message = await unitOfWork.Messages.GetSingleOrDefaultAsync(ownerId, cancellationToken);
        if (message == null)
        {
            return false;
        }

        var participants = await unitOfWork.Participants.GetListAsync(
            new ParticipantQueryOptions
            {
                ChatId = message.ChatId,
                UserId = userId,
            },
            cancellationToken);

        return participants.Any();
    }
}
