using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Services.Security.Permissions.Catalogs;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Events;
using Services.Shared.Models;
using Services.Socium.Enums;
using Services.Socium.Events;
using Services.Socium.Mapping;
using Services.Socium.Models;
using Services.Socium.Normalizers;
using Services.Socium.Policies;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

public interface IServiceRoom
{
    Task<ResponseInfo<RoomCreatePageResponse>> DisplayCreatePageAsync(RoomCreatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomCreatePageResponse>> CreateAsync(RoomCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<object>> AssignRoomAdminStageAsync(RoomAssignRoomAdminStageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomUpdatePageResponse>> DisplayUpdatePageAsync(RoomUpdatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomUpdatePageResponse>> UpdateAsync(RoomUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomDeletePageResponse>> DisplayDeletePageAsync(RoomDeletePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomDeleteResponse>> DeleteAsync(RoomDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomInfoPageResponse>> DisplayInfoPageAsync(RoomInfoPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoomListPageResponse>> DisplayListPageAsync(RoomListPageRequest request, CancellationToken cancellationToken = default);
}

public class ServiceRoom : IServiceRoom
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IRoomPolicy roomPolicy;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly ILogger<ServiceRoom> logger;

    public ServiceRoom(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IRoomPolicy roomPolicy,
        IDomainEventPublisher domainEventPublisher,
        ILogger<ServiceRoom> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.roomPolicy = roomPolicy;
        this.domainEventPublisher = domainEventPublisher;
        this.logger = logger;
    }

    public async Task<ResponseInfo<RoomCreatePageResponse>> DisplayCreatePageAsync(RoomCreatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.DisplayCreating);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryCreatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.BadRequest<RoomCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Unauthorized<RoomCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var canCreate = await roomPolicy.CanCreateAsync(user!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessCreate(canCreate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Forbidden<RoomCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var model = unitOfWork.Rooms.GetNew();
            var response = RoomMapper.ToCreatePageResponse(model, canCreate: true);
            logger.LogInformation(RoomCrudTexts.Messages.Success.DisplayCreateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoomCrudTexts.Messages.Success.DisplayCreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.DisplayCreate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DisplayCreateError);
            return ResponseInfo.Error<RoomCreatePageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DisplayCreateError);
        }
    }

    public async Task<ResponseInfo<RoomCreatePageResponse>> CreateAsync(RoomCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<RoomCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<RoomCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var canCreate = await roomPolicy.CanCreateAsync(user!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessCreate(canCreate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<RoomCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = RoomCrudValidators.ValidateDomainCreateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<RoomCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var nameNormalized = RoomNormalizer.Name(request.Name);
            var duplicateOptions = RoomMapper.ToQueryOptions(name: nameNormalized);
            var duplicates = await unitOfWork.Rooms.GetListAsync(duplicateOptions, cancellationToken);
            var duplicateErrors = RoomCrudValidators.ValidateDuplicates(duplicates.Any());
            if (duplicateErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, duplicateErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<RoomCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var model = unitOfWork.Rooms.GetNew();
            RoomMapper.Apply(model, nameNormalized);
            await unitOfWork.Rooms.CreateAsync(model, cancellationToken);
            var roomCreated = RoomMapper.ToCreatedEvent(model.Id, user.Id);
            await domainEventPublisher.PublishAsync(roomCreated, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var roomChanged = RoomMapper.ToChangedEvent(RoomChangedKind.Created, model.Id, model.Name);
            await domainEventPublisher.PublishAsync(roomChanged, cancellationToken);

            logger.LogInformation(RoomCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success<RoomCreatePageResponse>(MessageType.SAVED, RoomCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<RoomCreatePageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<object>> AssignRoomAdminStageAsync(RoomAssignRoomAdminStageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.AssignRoomAdminStaging);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryAssignRoomAdminStageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.AssignRoomAdminStageError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.AssignRoomAdminStageError, errors);
                return ResponseInfo.BadRequest<object>(MessageType.BAD_REQUEST, messageText);
            }

            var model = unitOfWork.RoomRoleAssignments.GetNew();
            RoomRoleAssignmentMapper.Apply(model, request.UserId, request.RoomId, RoleNames.RoomAdmin);
            await unitOfWork.RoomRoleAssignments.CreateAsync(model, cancellationToken);

            logger.LogInformation(RoomCrudTexts.Messages.Success.AssignRoomAdminStageCompleted);
            return ResponseInfo.Success<object>(MessageType.SAVED, RoomCrudTexts.Messages.Success.AssignRoomAdminStageCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.AssignRoomAdminStage);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.AssignRoomAdminStageError);
            return ResponseInfo.Error<object>(MessageType.ERROR, RoomCrudTexts.Messages.Error.AssignRoomAdminStageError);
        }
    }

    public async Task<ResponseInfo<RoomUpdatePageResponse>> DisplayUpdatePageAsync(RoomUpdatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.DisplayUpdating);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryUpdatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.BadRequest<RoomUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Unauthorized<RoomUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = RoomCrudValidators.ValidateAccessibilityRoom(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.NotFound<RoomUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canUpdate = await roomPolicy.CanUpdateAsync(user!.Id, model!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessUpdate(canUpdate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Forbidden<RoomUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = RoomMapper.ToUpdatePageResponse(model, canUpdate: true);
            logger.LogInformation(RoomCrudTexts.Messages.Success.DisplayUpdateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoomCrudTexts.Messages.Success.DisplayUpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.DisplayUpdate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DisplayUpdateError);
            return ResponseInfo.Error<RoomUpdatePageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DisplayUpdateError);
        }
    }

    public async Task<ResponseInfo<RoomUpdatePageResponse>> UpdateAsync(RoomUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.Updating);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.BadRequest<RoomUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Unauthorized<RoomUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = RoomCrudValidators.ValidateAccessibilityRoom(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<RoomUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canUpdate = await roomPolicy.CanUpdateAsync(user!.Id, model!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessUpdate(canUpdate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Forbidden<RoomUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = RoomCrudValidators.ValidateDomainUpdateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<RoomUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            var nameNormalized = RoomNormalizer.Name(request.Name);
            var duplicateOptions = RoomMapper.ToQueryOptions(name: nameNormalized, excludeId: request.Id);
            var duplicates = await unitOfWork.Rooms.GetListAsync(duplicateOptions, cancellationToken);
            var duplicateErrors = RoomCrudValidators.ValidateDuplicates(duplicates.Any());
            if (duplicateErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, duplicateErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<RoomUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            RoomMapper.Apply(model, nameNormalized);
            await unitOfWork.Rooms.UpdateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var roomChanged = RoomMapper.ToChangedEvent(RoomChangedKind.Updated, model.Id, model.Name);
            await domainEventPublisher.PublishAsync(roomChanged, cancellationToken);

            logger.LogInformation(RoomCrudTexts.Messages.Success.UpdateCompleted);
            return ResponseInfo.Success<RoomUpdatePageResponse>(MessageType.SAVED, RoomCrudTexts.Messages.Success.UpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.Update);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.UpdateError);
            return ResponseInfo.Error<RoomUpdatePageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.UpdateError);
        }
    }

    public async Task<ResponseInfo<RoomDeletePageResponse>> DisplayDeletePageAsync(RoomDeletePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.DisplayDeleting);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryDeletePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.BadRequest<RoomDeletePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Unauthorized<RoomDeletePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = RoomCrudValidators.ValidateAccessibilityRoom(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<RoomDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canDelete = await roomPolicy.CanDeleteAsync(user!.Id, model!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessDelete(canDelete);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<RoomDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var chatOptions = ChatMapper.ToQueryOptions(roomId: model.Id);
            var chatCount = await unitOfWork.Chats.CountAsync(chatOptions, cancellationToken);
            var response = RoomMapper.ToDeletePageResponse(model, chatCount, canDelete: true);
            logger.LogInformation(RoomCrudTexts.Messages.Success.DisplayDeleteCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoomCrudTexts.Messages.Success.DisplayDeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.DisplayDelete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DisplayDeleteError);
            return ResponseInfo.Error<RoomDeletePageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DisplayDeleteError);
        }
    }

    public async Task<ResponseInfo<RoomDeleteResponse>> DeleteAsync(RoomDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<RoomDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<RoomDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = RoomCrudValidators.ValidateAccessibilityRoom(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<RoomDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canDelete = await roomPolicy.CanDeleteAsync(user!.Id, model!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessDelete(canDelete);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<RoomDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            var roomId = model.Id;
            var roomName = model.Name;
            await unitOfWork.Rooms.DeleteAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var roomChanged = RoomMapper.ToChangedEvent(RoomChangedKind.Deleted, roomId, roomName);
            await domainEventPublisher.PublishAsync(roomChanged, cancellationToken);

            var response = RoomMapper.ToDeleteResponse();
            logger.LogInformation(RoomCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, RoomCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<RoomDeleteResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DeleteError);
        }
    }

    public async Task<ResponseInfo<RoomInfoPageResponse>> DisplayInfoPageAsync(RoomInfoPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.DisplayInfoLoading);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryInfoPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.BadRequest<RoomInfoPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Unauthorized<RoomInfoPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = RoomCrudValidators.ValidateAccessibilityRoom(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<RoomInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRead = await roomPolicy.CanReadAsync(user!.Id, model!.Id, cancellationToken);
            var accessErrors = RoomCrudValidators.ValidateAccessRead(canRead);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Forbidden<RoomInfoPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var canUpdate = await roomPolicy.CanUpdateAsync(user.Id, model.Id, cancellationToken);
            var canDelete = await roomPolicy.CanDeleteAsync(user.Id, model.Id, cancellationToken);
            var response = RoomMapper.ToInfoPageResponse(model, canUpdate, canDelete);
            logger.LogInformation(RoomCrudTexts.Messages.Success.DisplayInfoCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoomCrudTexts.Messages.Success.DisplayInfoCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.DisplayInfo);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DisplayInfoError);
            return ResponseInfo.Error<RoomInfoPageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DisplayInfoError);
        }
    }

    public async Task<ResponseInfo<RoomListPageResponse>> DisplayListPageAsync(RoomListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoomCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = RoomCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<RoomListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoomCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoomCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoomCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<RoomListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var readableRoomIds = await roomPolicy.GetReadableRoomIdsAsync(user!.Id, cancellationToken);
            var roomOptions = RoomMapper.ToQueryOptions(ids: readableRoomIds);
            var models = await unitOfWork.Rooms.GetListAsync(roomOptions, cancellationToken);

            ICollection<RoomListModel> rows = [];
            foreach (var model in models)
            {
                var canUpdate = await roomPolicy.CanUpdateAsync(user.Id, model.Id, cancellationToken);
                var canDelete = await roomPolicy.CanDeleteAsync(user.Id, model.Id, cancellationToken);
                rows.Add(RoomMapper.ToListModel(model, canUpdate, canDelete));
            }

            var response = RoomMapper.ToListPageResponse(rows);

            logger.LogInformation(RoomCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoomCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoomCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoomCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<RoomListPageResponse>(MessageType.ERROR, RoomCrudTexts.Messages.Error.DisplayListError);
        }
    }
}
