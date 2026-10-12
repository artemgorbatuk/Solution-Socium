using Services.Security.Api;
using Services.Shared.Enums;
using Services.Socium.Ports;

namespace WebApi.Ports;

public class UserRolePort : IUserRolePort
{
    private static readonly IReadOnlyList<string> Empty = [];

    private readonly IServiceAppRole serviceAppRole;

    public UserRolePort(IServiceAppRole serviceAppRole)
    {
        this.serviceAppRole = serviceAppRole;
    }

    public async Task<IReadOnlyList<string>> GetAppRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var responseInfo = await serviceAppRole.GetRoleNamesAsync(userId, cancellationToken);
        if (responseInfo.MessageInfo.MessageType == MessageType.ERROR)
        {
            throw new InvalidOperationException(responseInfo.MessageInfo.MessageText);
        }

        return responseInfo.Response ?? Empty;
    }
}
