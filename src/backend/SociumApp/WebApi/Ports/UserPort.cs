using Services.Security.Api;
using Services.Security.Models;
using Services.Shared.Enums;
using Services.Socium.Ports;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Socium к сервису пользователей Security. «Не найден» и «не вошёл» — <c>null</c>;
/// сбой Security — исключение, чтобы вызывающий сервис ответил ошибкой, а не «удалённым пользователем».
/// </summary>
public class UserPort : IUserPort
{
    private static readonly IReadOnlyDictionary<Guid, UserPortModel> Empty = new Dictionary<Guid, UserPortModel>();

    private readonly IServiceUser serviceUser;

    public UserPort(IServiceUser serviceUser)
    {
        this.serviceUser = serviceUser;
    }

    public async Task<UserPortModel?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = new UserInfoPageRequest { Id = id };
        var responseInfo = await serviceUser.DisplayInfoPageAsync(request, cancellationToken);
        ThrowIfError(responseInfo.MessageInfo.MessageType, responseInfo.MessageInfo.MessageText);

        return responseInfo.Response is { } user
            ? new UserPortModel { Id = user.Id, Login = user.Login, Name = user.Name }
            : null;
    }

    public async Task<IReadOnlyDictionary<Guid, UserPortModel>> GetListAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        ICollection<Guid> distinctIds = [.. ids.Distinct()];
        if (distinctIds.Count == 0)
        {
            return Empty;
        }

        var request = new UserListPageRequest { Ids = distinctIds, IncludeDeleted = true };
        var responseInfo = await serviceUser.DisplayListPageAsync(request, cancellationToken);
        ThrowIfError(responseInfo.MessageInfo.MessageType, responseInfo.MessageInfo.MessageText);

        return responseInfo.Response?.Rows.ToDictionary(
            row => row.Id,
            row => new UserPortModel { Id = row.Id, Login = row.Login, Name = row.Name, IsDeleted = row.IsDeleted }) ?? Empty;
    }

    private static void ThrowIfError(int messageType, string messageText)
    {
        if (messageType == MessageType.ERROR)
        {
            throw new InvalidOperationException(messageText);
        }
    }
}
