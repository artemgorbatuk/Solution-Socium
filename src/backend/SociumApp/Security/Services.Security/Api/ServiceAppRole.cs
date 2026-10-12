using Microsoft.Extensions.Logging;
using Repositories.Security.Ef.Api;
using Services.Security.Mapping;
using Services.Security.Models;
using Services.Security.Policies;
using Services.Security.Ports;
using Services.Security.Texts;
using Services.Security.Validation;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Security.Api;

/// <summary>
/// Роли приложения. Выдать роль можно только другому пользователю; снять — и с себя,
/// но последний админ приложения остаётся всегда.
/// </summary>
public interface IServiceAppRole
{
    Task<ResponseInfo<string[]>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AppRoleCreateResponse>> CreateAsync(AppRoleCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AppRoleDeleteResponse>> DeleteAsync(AppRoleDeleteRequest request, CancellationToken cancellationToken = default);
}

public class ServiceAppRole : IServiceAppRole
{
    private readonly IUnitOfWorkSecurity unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPolicy userPolicy;
    private readonly IUserNotifyPort userNotifyPort;
    private readonly ILogger<ServiceAppRole> logger;

    public ServiceAppRole(
        IUnitOfWorkSecurity unitOfWork,
        ICurrentUser currentUser,
        IUserPolicy userPolicy,
        IUserNotifyPort userNotifyPort,
        ILogger<ServiceAppRole> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPolicy = userPolicy;
        this.userNotifyPort = userNotifyPort;
        this.logger = logger;
    }

    public async Task<ResponseInfo<string[]>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var roleNames = await unitOfWork.AppRoleAssignments.GetRoleNamesAsync(userId, cancellationToken);
            return ResponseInfo.Success<string[]>([.. roleNames], MessageType.LOADED, AppRoleCrudTexts.Messages.Success.GetRoleNames);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AppRoleCrudTexts.Messages.Canceled.GetRoleNames);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AppRoleCrudTexts.Messages.Error.GetRoleNames);
            return ResponseInfo.Error<string[]>(MessageType.ERROR, AppRoleCrudTexts.Messages.Error.GetRoleNames);
        }
    }

    public async Task<ResponseInfo<AppRoleCreateResponse>> CreateAsync(AppRoleCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AppRoleCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = AppRoleCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<AppRoleCreateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = AppRoleCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<AppRoleCreateResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var target = await unitOfWork.Users.GetSingleOrDefaultAsync(request.UserId, cancellationToken);
            var accessibilityErrors = AppRoleCrudValidators.ValidateAccessibilityUser(target);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<AppRoleCreateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canGrant = await userPolicy.CanGrantAppAdminAsync(user!.Id, cancellationToken);
            var accessErrors = AppRoleCrudValidators.ValidateAccessCreate(canGrant, target!.Id, user.Id);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<AppRoleCreateResponse>(MessageType.FORBIDDEN, messageText);
            }

            var existing = await unitOfWork.AppRoleAssignments.GetSingleOrDefaultAsync(target.Id, request.RoleName, cancellationToken);
            var domainErrors = AppRoleCrudValidators.ValidateDomainCreate(existing is not null);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<AppRoleCreateResponse>(MessageType.INVALID, messageText);
            }

            var model = unitOfWork.AppRoleAssignments.GetNew();
            AppRoleMapper.Apply(model, target.Id, request.RoleName);
            await unitOfWork.AppRoleAssignments.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await userNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

            var response = AppRoleMapper.ToCreateResponse(model);
            logger.LogInformation(AppRoleCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, AppRoleCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AppRoleCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AppRoleCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<AppRoleCreateResponse>(MessageType.ERROR, AppRoleCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<AppRoleDeleteResponse>> DeleteAsync(AppRoleDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AppRoleCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = AppRoleCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<AppRoleDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = AppRoleCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<AppRoleDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var target = await unitOfWork.Users.GetSingleOrDefaultAsync(request.UserId, cancellationToken);
            var accessibilityErrors = AppRoleCrudValidators.ValidateAccessibilityUser(target);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<AppRoleDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRevoke = await userPolicy.CanRevokeAppAdminAsync(user!.Id, cancellationToken);
            var accessErrors = AppRoleCrudValidators.ValidateAccessDelete(canRevoke);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<AppRoleDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            var model = await unitOfWork.AppRoleAssignments.GetSingleOrDefaultAsync(target!.Id, request.RoleName, cancellationToken);
            var roleOptions = AppRoleMapper.ToQueryOptions(roleName: request.RoleName);
            var roleAssignments = await unitOfWork.AppRoleAssignments.GetListAsync(roleOptions, cancellationToken);
            var domainErrors = AppRoleCrudValidators.ValidateDomainDelete(model is not null, roleAssignments.Count());
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{AppRoleCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AppRoleCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Invalid<AppRoleDeleteResponse>(MessageType.INVALID, messageText);
            }

            await unitOfWork.AppRoleAssignments.DeleteAsync(model!, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await userNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

            var response = AppRoleMapper.ToDeleteResponse();
            logger.LogInformation(AppRoleCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, AppRoleCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AppRoleCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AppRoleCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<AppRoleDeleteResponse>(MessageType.ERROR, AppRoleCrudTexts.Messages.Error.DeleteError);
        }
    }
}
