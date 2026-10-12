using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Services.Security.Permissions.Catalogs;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Events;
using Services.Shared.Models;
using Services.Socium.Enums;
using Services.Socium.Mapping;
using Services.Socium.Models;
using Services.Socium.Normalizers;
using Services.Socium.Policies;
using Services.Socium.Ports;
using Services.Socium.Providers;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

public interface IServiceChat
{
    Task<ResponseInfo<ChatCreatePageResponse>> DisplayCreatePageAsync(ChatCreatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatCreatePageResponse>> CreateAsync(ChatCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatCreatePageResponse>> CreateStageAsync(ChatCreateStageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatUpdatePageResponse>> DisplayUpdatePageAsync(ChatUpdatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatUpdatePageResponse>> UpdateAsync(ChatUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatDeletePageResponse>> DisplayDeletePageAsync(ChatDeletePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatDeleteResponse>> DeleteAsync(ChatDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatInfoPageResponse>> DisplayInfoPageAsync(ChatInfoPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatListPageResponse>> DisplayListPageAsync(ChatListPageRequest request, CancellationToken cancellationToken = default);
}

public class ServiceChat : IServiceChat
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IFilePort filePort;
    private readonly IChatPolicy chatPolicy;
    private readonly IRoleAssignmentProvider roleAssignmentProvider;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly ILogger<ServiceChat> logger;

    public ServiceChat(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IFilePort filePort,
        IChatPolicy chatPolicy,
        IRoleAssignmentProvider roleAssignmentProvider,
        IDomainEventPublisher domainEventPublisher,
        ILogger<ServiceChat> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.filePort = filePort;
        this.chatPolicy = chatPolicy;
        this.roleAssignmentProvider = roleAssignmentProvider;
        this.domainEventPublisher = domainEventPublisher;
        this.logger = logger;
    }

    public async Task<ResponseInfo<ChatCreatePageResponse>> DisplayCreatePageAsync(ChatCreatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.DisplayCreating);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryCreatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.BadRequest<ChatCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Unauthorized<ChatCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var room = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.RoomId, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityRoom(room);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.NotFound<ChatCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canCreate = await chatPolicy.CanCreateAsync(user!.Id, request.RoomId, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessCreate(canCreate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Forbidden<ChatCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var newOptions = ChatMapper.ToGetNewOptions(request.RoomId);
            var model = unitOfWork.Chats.GetNew(newOptions);
            var response = ChatMapper.ToCreatePageResponse(model, canCreate: true);
            logger.LogInformation(ChatCrudTexts.Messages.Success.DisplayCreateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ChatCrudTexts.Messages.Success.DisplayCreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.DisplayCreate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DisplayCreateError);
            return ResponseInfo.Error<ChatCreatePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DisplayCreateError);
        }
    }

    public async Task<ResponseInfo<ChatCreatePageResponse>> CreateAsync(ChatCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<ChatCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<ChatCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var room = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.RoomId, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityRoom(room);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<ChatCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canCreate = await chatPolicy.CanCreateAsync(user!.Id, request.RoomId, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessCreate(canCreate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<ChatCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var stageRequest = ChatMapper.ToCreateStageRequest(request.RoomId, user.Id, request.Name);
            var staged = await CreateStageAsync(stageRequest, cancellationToken);
            if (staged.MessageInfo.MessageType != MessageType.SAVED)
            {
                return staged;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var created = staged.Response!;
            var chatChanged = ChatMapper.ToChangedEvent(ChatChangedKind.Created, created.Id, created.RoomId, created.Name);
            await domainEventPublisher.PublishAsync(chatChanged, cancellationToken);

            logger.LogInformation(ChatCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success<ChatCreatePageResponse>(MessageType.SAVED, ChatCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<ChatCreatePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<ChatCreatePageResponse>> CreateStageAsync(ChatCreateStageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.CreateStaging);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryCreateStageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateStageError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateStageError, errors);
                return ResponseInfo.BadRequest<ChatCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var creator = await userPort.GetSingleOrDefaultAsync(request.CreatorUserId, cancellationToken);
            var creatorErrors = ChatCrudValidators.ValidateCurrentUser(creator);
            if (creatorErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, creatorErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateStageError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateStageError, errors);
                return ResponseInfo.Unauthorized<ChatCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var domainErrors = ChatCrudValidators.ValidateDomainCreateStageRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateStageError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateStageError, errors);
                return ResponseInfo.Invalid<ChatCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var nameNormalized = ChatNormalizer.Name(request.Name);
            var chatNewOptions = ChatMapper.ToGetNewOptions(request.RoomId);
            var model = unitOfWork.Chats.GetNew(chatNewOptions);
            ChatMapper.Apply(model, nameNormalized);
            await unitOfWork.Chats.CreateAsync(model, cancellationToken);

            var chatCreated = ChatMapper.ToCreatedEvent(model.Id, creator!.Id);
            await domainEventPublisher.PublishAsync(chatCreated, cancellationToken);

            var response = ChatMapper.ToCreatePageResponse(model, canCreate: true);
            logger.LogInformation(ChatCrudTexts.Messages.Success.CreateStageCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ChatCrudTexts.Messages.Success.CreateStageCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.CreateStage);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.CreateStageError);
            return ResponseInfo.Error<ChatCreatePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.CreateStageError);
        }
    }

    public async Task<ResponseInfo<ChatUpdatePageResponse>> DisplayUpdatePageAsync(ChatUpdatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.DisplayUpdating);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryUpdatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.BadRequest<ChatUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Unauthorized<ChatUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.NotFound<ChatUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canUpdate = await chatPolicy.CanUpdateAsync(user!.Id, model!.RoomId, model.Id, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessUpdate(canUpdate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Forbidden<ChatUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = ChatMapper.ToUpdatePageResponse(model, canUpdate: true);
            logger.LogInformation(ChatCrudTexts.Messages.Success.DisplayUpdateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ChatCrudTexts.Messages.Success.DisplayUpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.DisplayUpdate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DisplayUpdateError);
            return ResponseInfo.Error<ChatUpdatePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DisplayUpdateError);
        }
    }

    public async Task<ResponseInfo<ChatUpdatePageResponse>> UpdateAsync(ChatUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.Updating);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.BadRequest<ChatUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Unauthorized<ChatUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<ChatUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canUpdate = await chatPolicy.CanUpdateAsync(user!.Id, model!.RoomId, model.Id, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessUpdate(canUpdate);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Forbidden<ChatUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = ChatCrudValidators.ValidateDomainUpdateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<ChatUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            var nameNormalized = ChatNormalizer.Name(request.Name);
            ChatMapper.Apply(model, nameNormalized);
            await unitOfWork.Chats.UpdateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var chatChanged = ChatMapper.ToChangedEvent(ChatChangedKind.Updated, model.Id, model.RoomId, model.Name);
            await domainEventPublisher.PublishAsync(chatChanged, cancellationToken);

            logger.LogInformation(ChatCrudTexts.Messages.Success.UpdateCompleted);
            return ResponseInfo.Success<ChatUpdatePageResponse>(MessageType.SAVED, ChatCrudTexts.Messages.Success.UpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.Update);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.UpdateError);
            return ResponseInfo.Error<ChatUpdatePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.UpdateError);
        }
    }

    public async Task<ResponseInfo<ChatDeletePageResponse>> DisplayDeletePageAsync(ChatDeletePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.DisplayDeleting);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryDeletePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.BadRequest<ChatDeletePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Unauthorized<ChatDeletePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<ChatDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canDelete = await chatPolicy.CanDeleteAsync(user!.Id, model!.RoomId, model.Id, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessDelete(canDelete);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<ChatDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = ChatMapper.ToDeletePageResponse(model, canDelete: true);
            logger.LogInformation(ChatCrudTexts.Messages.Success.DisplayDeleteCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ChatCrudTexts.Messages.Success.DisplayDeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.DisplayDelete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DisplayDeleteError);
            return ResponseInfo.Error<ChatDeletePageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DisplayDeleteError);
        }
    }

    public async Task<ResponseInfo<ChatDeleteResponse>> DeleteAsync(ChatDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<ChatDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<ChatDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<ChatDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canDelete = await chatPolicy.CanDeleteAsync(user!.Id, model!.RoomId, model.Id, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessDelete(canDelete);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<ChatDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            var chatId = model.Id;
            var roomId = model.RoomId;
            var chatName = model.Name;
            await unitOfWork.Chats.DeleteAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var chatChanged = ChatMapper.ToChangedEvent(ChatChangedKind.Deleted, chatId, roomId, chatName);
            await domainEventPublisher.PublishAsync(chatChanged, cancellationToken);

            var response = ChatMapper.ToDeleteResponse();
            logger.LogInformation(ChatCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ChatCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<ChatDeleteResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DeleteError);
        }
    }

    public async Task<ResponseInfo<ChatInfoPageResponse>> DisplayInfoPageAsync(ChatInfoPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.DisplayInfoLoading);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryInfoPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.BadRequest<ChatInfoPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Unauthorized<ChatInfoPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<ChatInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRead = await chatPolicy.CanReadAsync(user!.Id, model!.RoomId, model.Id, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessRead(canRead);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Forbidden<ChatInfoPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var canUpdate = await chatPolicy.CanUpdateAsync(user.Id, model.RoomId, model.Id, cancellationToken);
            var canDelete = await chatPolicy.CanDeleteAsync(user.Id, model.RoomId, model.Id, cancellationToken);
            var chatRoles = await roleAssignmentProvider.GetChatRolesAsync(user.Id, model.Id, cancellationToken);
            var isParticipant = chatRoles.Contains(RoleNames.ChatParticipant, StringComparer.Ordinal);
            var isAdmin = chatRoles.Contains(RoleNames.ChatAdmin, StringComparer.Ordinal);
            var response = ChatMapper.ToInfoPageResponse(model, isParticipant, isAdmin, canUpdate, canDelete);
            logger.LogInformation(ChatCrudTexts.Messages.Success.DisplayInfoCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ChatCrudTexts.Messages.Success.DisplayInfoCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.DisplayInfo);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DisplayInfoError);
            return ResponseInfo.Error<ChatInfoPageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DisplayInfoError);
        }
    }

    public async Task<ResponseInfo<ChatListPageResponse>> DisplayListPageAsync(ChatListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ChatCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = ChatCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<ChatListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ChatCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<ChatListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var room = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.RoomId, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityRoom(room);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<ChatListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var readableChatIds = (await chatPolicy.GetReadableChatIdsAsync(user!.Id, request.RoomId, cancellationToken)).ToHashSet();
            var chatOptions = ChatMapper.ToQueryOptions(roomId: request.RoomId);
            var models = (await unitOfWork.Chats.GetListAsync(chatOptions, cancellationToken))
                .Where(model => readableChatIds.Contains(model.Id))
                .ToList();
            var chatIds = models.Select(model => model.Id).ToList();
            var lastByChat = await unitOfWork.Messages.GetLastByChatIdsAsync(chatIds, cancellationToken);
            var unreadByChat = await unitOfWork.Recipients.GetUnreadCountsByChatIdsAsync(user.Id, chatIds, cancellationToken);
            var senderIds = lastByChat.Values.Select(row => row.SenderUserId).Distinct().ToList();
            var senders = senderIds.Count == 0
                ? new Dictionary<Guid, UserPortModel>()
                : await userPort.GetListAsync(senderIds, cancellationToken);
            var attachmentMessageIds = lastByChat.Values.Select(row => row.MessageId).ToList();
            var attachmentsByMessage = attachmentMessageIds.Count == 0
                ? new Dictionary<Guid, IReadOnlyList<FilePortAttachment>>()
                : await filePort.GetByMessageIdsAsync(attachmentMessageIds, cancellationToken);

            ICollection<ChatListModel> rows = [];
            foreach (var model in models)
            {
                lastByChat.TryGetValue(model.Id, out var last);
                unreadByChat.TryGetValue(model.Id, out var unread);
                var hasAttachments = last is not null
                    && (attachmentsByMessage.GetValueOrDefault(last.MessageId)?.Count ?? 0) > 0;
                var previewText = last is null
                    ? null
                    : string.IsNullOrWhiteSpace(last.Text)
                        ? (hasAttachments ? "Вложение" : null)
                        : ChatMapper.TruncatePreview(last.Text);
                var senderName = last is null
                    ? null
                    : senders.GetValueOrDefault(last.SenderUserId)?.Name;
                var canUpdate = await chatPolicy.CanUpdateAsync(user.Id, model.RoomId, model.Id, cancellationToken);
                var canDelete = await chatPolicy.CanDeleteAsync(user.Id, model.RoomId, model.Id, cancellationToken);
                rows.Add(ChatMapper.ToListModel(
                    model,
                    canUpdate,
                    canDelete,
                    unread,
                    previewText,
                    last?.CreatedAt,
                    senderName,
                    last?.SenderUserId,
                    hasAttachments));
            }

            rows =
            [
                .. rows
                    // Пустой чат — сверху (MaxValue); среди них новее Id (v7) выше. С сообщениями — по lastMessageAt.
                    .OrderByDescending(row => row.LastMessageAt ?? DateTime.MaxValue)
                    .ThenByDescending(row => row.Id),
            ];
            var response = ChatMapper.ToListPageResponse(rows);

            logger.LogInformation(ChatCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ChatCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ChatCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ChatCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<ChatListPageResponse>(MessageType.ERROR, ChatCrudTexts.Messages.Error.DisplayListError);
        }
    }
}
