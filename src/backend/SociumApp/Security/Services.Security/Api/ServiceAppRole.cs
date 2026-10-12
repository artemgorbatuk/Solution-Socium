using Microsoft.Extensions.Logging;
using Repositories.Security.Ef.Api;
using Services.Security.Texts;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Security.Api;

public interface IServiceAppRole
{
    Task<ResponseInfo<string[]>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class ServiceAppRole : IServiceAppRole
{
    private readonly IUnitOfWorkSecurity unitOfWork;
    private readonly ILogger<ServiceAppRole> logger;

    public ServiceAppRole(IUnitOfWorkSecurity unitOfWork, ILogger<ServiceAppRole> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ResponseInfo<string[]>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var roleNames = await unitOfWork.AppRoleAssignments.GetRoleNamesAsync(userId, cancellationToken);
            return ResponseInfo.Success<string[]>([.. roleNames], MessageType.LOADED, AppRoleTexts.Messages.Success.GetRoleNames);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AppRoleTexts.Messages.Canceled.GetRoleNames);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AppRoleTexts.Messages.Error.GetRoleNames);
            return ResponseInfo.Error<string[]>(MessageType.ERROR, AppRoleTexts.Messages.Error.GetRoleNames);
        }
    }
}
