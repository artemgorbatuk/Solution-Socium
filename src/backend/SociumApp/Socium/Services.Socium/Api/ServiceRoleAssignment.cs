using Datasource.Socium.Ef.Enums;
using Datasource.Socium.Ef.Models;
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
using Services.Socium.Policies;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

/// <summary>
/// Доступы пользователя в комнатах и чатах для админа приложения (страница <c>/users/:id</c>).
/// Роли меняются через stage-методы <see cref="IServiceRoom"/> и <see cref="IServiceParticipant"/>,
/// чтобы инварианты чата не дублировались.
/// </summary>
public interface IServiceRoleAssignment
{
    Task<ResponseInfo<RoleAssignmentListPageResponse>> DisplayListPageAsync(RoleAssignmentListPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoleAssignmentCreateResponse>> CreateAsync(RoleAssignmentCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<RoleAssignmentDeleteResponse>> DeleteAsync(RoleAssignmentDeleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Для Security перед удалением пользователя (через порт), не для API.</summary>
    Task<ResponseInfo<RoleAssignmentLastAdminListResponse>> DisplayLastAdminListAsync(RoleAssignmentLastAdminListRequest request, CancellationToken cancellationToken = default);

    /// <summary>Для Security после удаления пользователя (через порт), не для API: текущий пользователь уже удалён.</summary>
    Task<ResponseInfo<RoleAssignmentDeleteByUserResponse>> DeleteByUserAsync(RoleAssignmentDeleteByUserRequest request, CancellationToken cancellationToken = default);
}

public class ServiceRoleAssignment : IServiceRoleAssignment
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IRoleAssignmentPolicy roleAssignmentPolicy;
    private readonly IServiceRoom serviceRoom;
    private readonly IServiceParticipant serviceParticipant;
    private readonly IChatNotifyPort chatNotifyPort;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceRoleAssignment> logger;

    public ServiceRoleAssignment(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IRoleAssignmentPolicy roleAssignmentPolicy,
        IServiceRoom serviceRoom,
        IServiceParticipant serviceParticipant,
        IChatNotifyPort chatNotifyPort,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider,
        ILogger<ServiceRoleAssignment> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.roleAssignmentPolicy = roleAssignmentPolicy;
        this.serviceRoom = serviceRoom;
        this.serviceParticipant = serviceParticipant;
        this.chatNotifyPort = chatNotifyPort;
        this.domainEventPublisher = domainEventPublisher;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <summary>Все комнаты и чаты: одним запросом — назначения пользователя, одним — админы по комнатам и чатам.</summary>
    public async Task<ResponseInfo<RoleAssignmentListPageResponse>> DisplayListPageAsync(RoleAssignmentListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = RoleAssignmentCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<RoleAssignmentListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoleAssignmentCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<RoleAssignmentListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var target = await GetUserWithDeletedAsync(request.UserId, cancellationToken);
            var accessibilityErrors = RoleAssignmentCrudValidators.ValidateAccessibilityUser(target);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<RoleAssignmentListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRead = await roleAssignmentPolicy.CanReadAsync(user!.Id, cancellationToken);
            var accessErrors = RoleAssignmentCrudValidators.ValidateAccessRead(canRead);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Forbidden<RoleAssignmentListPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var canManagePermission = await roleAssignmentPolicy.CanManageAsync(user.Id, cancellationToken);
            var canManage = !RoleAssignmentCrudValidators.ValidateAccessManage(canManagePermission, target!, user.Id).Any();

            var rooms = await unitOfWork.Rooms.GetListAsync(cancellationToken: cancellationToken);
            var chats = await unitOfWork.Chats.GetListAsync(cancellationToken: cancellationToken);
            var userRoomRoles = await unitOfWork.RoomRoleAssignments.GetListAsync(
                RoleAssignmentMapper.ToRoomRoleQueryOptions(userId: target!.Id),
                cancellationToken);
            var userChatRoles = await unitOfWork.ChatRoleAssignments.GetListAsync(
                RoleAssignmentMapper.ToChatRoleQueryOptions(userId: target.Id),
                cancellationToken);
            var roomAdminCounts = await GetRoomAdminCountsAsync(cancellationToken);
            var chatAdminCounts = await GetChatAdminCountsAsync(cancellationToken);

            var adminRoomIds = userRoomRoles
                .Where(assignment => assignment.RoleName == RoleNames.RoomAdmin)
                .Select(assignment => assignment.RoomId)
                .ToHashSet();
            var participantChatIds = userChatRoles
                .Where(assignment => assignment.RoleName == RoleNames.ChatParticipant)
                .Select(assignment => assignment.ChatId)
                .ToHashSet();
            var adminChatIds = userChatRoles
                .Where(assignment => assignment.RoleName == RoleNames.ChatAdmin)
                .Select(assignment => assignment.ChatId)
                .ToHashSet();
            var chatsByRoom = chats.ToLookup(chat => chat.RoomId);
            ICollection<RoleAssignmentRoomModel> roomRows =
            [
                .. rooms.Select(room => RoleAssignmentMapper.ToRoomModel(
                    room,
                    adminRoomIds.Contains(room.Id),
                    roomAdminCounts.GetValueOrDefault(room.Id),
                    [
                        .. chatsByRoom[room.Id].Select(chat => RoleAssignmentMapper.ToChatModel(
                            chat,
                            participantChatIds.Contains(chat.Id),
                            adminChatIds.Contains(chat.Id),
                            chatAdminCounts.GetValueOrDefault(chat.Id))),
                    ])),
            ];
            var response = RoleAssignmentMapper.ToListPageResponse(target.Id, canManage, roomRows);

            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoleAssignmentCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoleAssignmentCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<RoleAssignmentListPageResponse>(MessageType.ERROR, RoleAssignmentCrudTexts.Messages.Error.DisplayListError);
        }
    }

    public async Task<ResponseInfo<RoleAssignmentCreateResponse>> CreateAsync(RoleAssignmentCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = RoleAssignmentCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<RoleAssignmentCreateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoleAssignmentCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<RoleAssignmentCreateResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var target = await GetUserWithDeletedAsync(request.UserId, cancellationToken);
            var room = request.RoomId is Guid roomId
                ? await unitOfWork.Rooms.GetSingleOrDefaultAsync(roomId, cancellationToken)
                : null;
            var chat = request.ChatId is Guid chatId
                ? await unitOfWork.Chats.GetSingleOrDefaultAsync(chatId, cancellationToken)
                : null;
            IEnumerable<string> accessibilityErrors =
            [
                .. RoleAssignmentCrudValidators.ValidateAccessibilityUser(target),
                .. request.RoomId is null ? [] : RoleAssignmentCrudValidators.ValidateAccessibilityRoom(room),
                .. request.ChatId is null ? [] : RoleAssignmentCrudValidators.ValidateAccessibilityChat(chat),
            ];
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<RoleAssignmentCreateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canManage = await roleAssignmentPolicy.CanManageAsync(user!.Id, cancellationToken);
            var accessErrors = RoleAssignmentCrudValidators.ValidateAccessManage(canManage, target!, user.Id);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<RoleAssignmentCreateResponse>(MessageType.FORBIDDEN, messageText);
            }

            return request.RoleName switch
            {
                RoleNames.RoomAdmin => await CreateRoomAdminAsync(target!, room!, cancellationToken),
                RoleNames.ChatParticipant => await CreateParticipantAsync(target!, chat!, cancellationToken),
                _ => await CreateChatAdminAsync(target!, chat!, cancellationToken),
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoleAssignmentCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<RoleAssignmentCreateResponse>(MessageType.ERROR, RoleAssignmentCrudTexts.Messages.Error.CreateError);
        }
    }

    private async Task<ResponseInfo<RoleAssignmentCreateResponse>> CreateRoomAdminAsync(UserPortModel target, Room room, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.RoomRoleAssignments.GetSingleOrDefaultAsync(target.Id, room.Id, RoleNames.RoomAdmin, cancellationToken);
        var domainErrors = RoleAssignmentCrudValidators.ValidateDomainCreateRoomAdmin(existing is not null);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
            return ResponseInfo.Invalid<RoleAssignmentCreateResponse>(MessageType.INVALID, messageText);
        }

        var stageRequest = RoomRoleAssignmentMapper.ToAssignRoomAdminStageRequest(room.Id, target.Id);
        var staged = await serviceRoom.AssignRoomAdminStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentCreateResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        var response = RoleAssignmentMapper.ToCreateResponse(target.Id, RoleNames.RoomAdmin);
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
        return ResponseInfo.Success(response, MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
    }

    /// <summary>Как добавление участника из чата: в чате без админов новый участник становится админом.</summary>
    private async Task<ResponseInfo<RoleAssignmentCreateResponse>> CreateParticipantAsync(UserPortModel target, Chat chat, CancellationToken cancellationToken)
    {
        var participantOptions = ParticipantMapper.ToQueryOptions(chatId: chat.Id, userId: target.Id);
        var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
        var domainErrors = ParticipantCrudValidators.ValidateDomainCreateRequest(participants.SingleOrDefault());
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
            return ResponseInfo.Invalid<RoleAssignmentCreateResponse>(MessageType.INVALID, messageText);
        }

        var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(chat.Id, RoleNames.ChatAdmin, cancellationToken);
        var becameAdmin = adminCount == 0;
        var stageRequest = ParticipantMapper.ToCreateStageRequest(chat.Id, target.Id, becameAdmin);
        var staged = await serviceParticipant.CreateStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentCreateResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var joinedNotify = ParticipantMapper.ToNotifyParticipant(chat.Id, staged.Response!.Id, target.Id, target.Login, target.Name, becameAdmin);
        await chatNotifyPort.NotifyParticipantJoinedAsync(joinedNotify, cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        var response = RoleAssignmentMapper.ToCreateResponse(target.Id, RoleNames.ChatParticipant);
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
        return ResponseInfo.Success(response, MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
    }

    private async Task<ResponseInfo<RoleAssignmentCreateResponse>> CreateChatAdminAsync(UserPortModel target, Chat chat, CancellationToken cancellationToken)
    {
        var participantOptions = ParticipantMapper.ToQueryOptions(chatId: chat.Id, userId: target.Id);
        var participant = (await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken)).SingleOrDefault();
        var existing = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(target.Id, chat.Id, RoleNames.ChatAdmin, cancellationToken);
        var domainErrors = RoleAssignmentCrudValidators.ValidateDomainCreateChatAdmin(participant, existing is not null);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.CreateError, errors);
            return ResponseInfo.Invalid<RoleAssignmentCreateResponse>(MessageType.INVALID, messageText);
        }

        var stageRequest = ParticipantMapper.ToUpdateStageRequest(participant!.Id, isAdmin: true);
        var staged = await serviceParticipant.UpdateStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentCreateResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var roleChangedNotify = ParticipantMapper.ToNotifyParticipant(chat.Id, participant.Id, target.Id, target.Login, target.Name, isAdmin: true);
        await chatNotifyPort.NotifyParticipantRoleChangedAsync(roleChangedNotify, cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        var response = RoleAssignmentMapper.ToCreateResponse(target.Id, RoleNames.ChatAdmin);
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
        return ResponseInfo.Success(response, MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.CreateCompleted);
    }

    public async Task<ResponseInfo<RoleAssignmentDeleteResponse>> DeleteAsync(RoleAssignmentDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = RoleAssignmentCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<RoleAssignmentDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoleAssignmentCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<RoleAssignmentDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var target = await GetUserWithDeletedAsync(request.UserId, cancellationToken);
            var room = request.RoomId is Guid roomId
                ? await unitOfWork.Rooms.GetSingleOrDefaultAsync(roomId, cancellationToken)
                : null;
            var chat = request.ChatId is Guid chatId
                ? await unitOfWork.Chats.GetSingleOrDefaultAsync(chatId, cancellationToken)
                : null;
            IEnumerable<string> accessibilityErrors =
            [
                .. RoleAssignmentCrudValidators.ValidateAccessibilityUser(target),
                .. request.RoomId is null ? [] : RoleAssignmentCrudValidators.ValidateAccessibilityRoom(room),
                .. request.ChatId is null ? [] : RoleAssignmentCrudValidators.ValidateAccessibilityChat(chat),
            ];
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<RoleAssignmentDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canManage = await roleAssignmentPolicy.CanManageAsync(user!.Id, cancellationToken);
            var accessErrors = RoleAssignmentCrudValidators.ValidateAccessManage(canManage, target!, user.Id);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<RoleAssignmentDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            return request.RoleName switch
            {
                RoleNames.RoomAdmin => await DeleteRoomAdminAsync(target!, room!, cancellationToken),
                RoleNames.ChatParticipant => await DeleteParticipantAsync(target!, chat!, cancellationToken),
                _ => await DeleteChatAdminAsync(target!, chat!, cancellationToken),
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoleAssignmentCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<RoleAssignmentDeleteResponse>(MessageType.ERROR, RoleAssignmentCrudTexts.Messages.Error.DeleteError);
        }
    }

    private async Task<ResponseInfo<RoleAssignmentDeleteResponse>> DeleteRoomAdminAsync(UserPortModel target, Room room, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.RoomRoleAssignments.GetSingleOrDefaultAsync(target.Id, room.Id, RoleNames.RoomAdmin, cancellationToken);
        var adminCount = await unitOfWork.RoomRoleAssignments.CountByRoomAndRoleAsync(room.Id, RoleNames.RoomAdmin, cancellationToken);
        var domainErrors = RoleAssignmentCrudValidators.ValidateDomainDeleteRoomAdmin(existing is not null, adminCount);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Invalid<RoleAssignmentDeleteResponse>(MessageType.INVALID, messageText);
        }

        var stageRequest = RoomRoleAssignmentMapper.ToRevokeRoomAdminStageRequest(room.Id, target.Id);
        var staged = await serviceRoom.RevokeRoomAdminStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentDeleteResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
        return ResponseInfo.Success(RoleAssignmentMapper.ToDeleteResponse(), MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
    }

    /// <summary>Как исключение из чата: снимаются все роли чата и строка участника.</summary>
    private async Task<ResponseInfo<RoleAssignmentDeleteResponse>> DeleteParticipantAsync(UserPortModel target, Chat chat, CancellationToken cancellationToken)
    {
        var participantOptions = ParticipantMapper.ToQueryOptions(chatId: chat.Id, userId: target.Id);
        var participant = (await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken)).SingleOrDefault();
        var isAdmin = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(target.Id, chat.Id, RoleNames.ChatAdmin, cancellationToken) is not null;
        var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(chat.Id, RoleNames.ChatAdmin, cancellationToken);
        var domainErrors = RoleAssignmentCrudValidators.ValidateDomainDeleteParticipant(participant, isAdmin, adminCount);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Invalid<RoleAssignmentDeleteResponse>(MessageType.INVALID, messageText);
        }

        var stageRequest = ParticipantMapper.ToDeleteStageRequest(participant!.Id);
        var staged = await serviceParticipant.DeleteStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentDeleteResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await chatNotifyPort.NotifyParticipantLeftAsync(chat.Id, participant.Id, target.Id, cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
        return ResponseInfo.Success(RoleAssignmentMapper.ToDeleteResponse(), MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
    }

    private async Task<ResponseInfo<RoleAssignmentDeleteResponse>> DeleteChatAdminAsync(UserPortModel target, Chat chat, CancellationToken cancellationToken)
    {
        var isAdmin = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(target.Id, chat.Id, RoleNames.ChatAdmin, cancellationToken) is not null;
        var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(chat.Id, RoleNames.ChatAdmin, cancellationToken);
        var participantOptions = ParticipantMapper.ToQueryOptions(chatId: chat.Id, userId: target.Id);
        var participant = (await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken)).SingleOrDefault();
        IEnumerable<string> domainErrors =
        [
            .. RoleAssignmentCrudValidators.ValidateDomainDeleteChatAdmin(isAdmin, adminCount),
            .. isAdmin ? ParticipantCrudValidators.ValidateAccessParticipant(participant) : [],
        ];
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Invalid<RoleAssignmentDeleteResponse>(MessageType.INVALID, messageText);
        }

        var stageRequest = ParticipantMapper.ToUpdateStageRequest(participant!.Id, isAdmin: false);
        var staged = await serviceParticipant.UpdateStageAsync(stageRequest, cancellationToken);
        if (staged.MessageInfo.MessageType != MessageType.SAVED)
        {
            return ResponseInfo.Error<RoleAssignmentDeleteResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var roleChangedNotify = ParticipantMapper.ToNotifyParticipant(chat.Id, participant.Id, target.Id, target.Login, target.Name, isAdmin: false);
        await chatNotifyPort.NotifyParticipantRoleChangedAsync(roleChangedNotify, cancellationToken);
        await chatNotifyPort.NotifyUserAccessChangedAsync(target.Id, cancellationToken);

        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
        return ResponseInfo.Success(RoleAssignmentMapper.ToDeleteResponse(), MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.DeleteCompleted);
    }

    public async Task<ResponseInfo<RoleAssignmentLastAdminListResponse>> DisplayLastAdminListAsync(RoleAssignmentLastAdminListRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Start.DisplayLastAdminListLoading);

        try
        {
            var primaryErrors = RoleAssignmentCrudValidators.ValidatePrimaryLastAdminListRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError, errors);
                return ResponseInfo.BadRequest<RoleAssignmentLastAdminListResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = RoleAssignmentCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError, errors);
                return ResponseInfo.Unauthorized<RoleAssignmentLastAdminListResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var rooms = (await unitOfWork.Rooms.GetListAsync(cancellationToken: cancellationToken)).ToDictionary(room => room.Id);
            var chats = await unitOfWork.Chats.GetListAsync(cancellationToken: cancellationToken);
            var userRoomRoles = await unitOfWork.RoomRoleAssignments.GetListAsync(
                RoleAssignmentMapper.ToRoomRoleQueryOptions(userId: request.UserId, roleName: RoleNames.RoomAdmin),
                cancellationToken);
            var userChatRoles = await unitOfWork.ChatRoleAssignments.GetListAsync(
                RoleAssignmentMapper.ToChatRoleQueryOptions(userId: request.UserId, roleName: RoleNames.ChatAdmin),
                cancellationToken);
            var roomAdminCounts = await GetRoomAdminCountsAsync(cancellationToken);
            var chatAdminCounts = await GetChatAdminCountsAsync(cancellationToken);

            var adminRoomIds = userRoomRoles.Select(assignment => assignment.RoomId).ToHashSet();
            var adminChatIds = userChatRoles.Select(assignment => assignment.ChatId).ToHashSet();
            ICollection<string> places =
            [
                .. rooms.Values
                    .Where(room => RoleAssignmentCrudValidators
                        .ValidateDomainLastRoomAdmin(adminRoomIds.Contains(room.Id), roomAdminCounts.GetValueOrDefault(room.Id))
                        .Any())
                    .Select(room => RoleAssignmentCrudTexts.Messages.Validation.RoomPlace(room.Name)),
                .. chats
                    .Where(chat => ParticipantCrudValidators
                        .ValidateDomainLastChatAdmin(adminChatIds.Contains(chat.Id), chatAdminCounts.GetValueOrDefault(chat.Id))
                        .Any())
                    .Select(chat => RoleAssignmentCrudTexts.Messages.Validation.ChatPlace(rooms[chat.RoomId].Name, chat.Name)),
            ];
            var response = RoleAssignmentMapper.ToLastAdminListResponse(places);

            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DisplayLastAdminListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, RoleAssignmentCrudTexts.Messages.Success.DisplayLastAdminListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Canceled.DisplayLastAdminList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError);
            return ResponseInfo.Error<RoleAssignmentLastAdminListResponse>(MessageType.ERROR, RoleAssignmentCrudTexts.Messages.Error.DisplayLastAdminListError);
        }
    }

    /// <summary>Членство, роли комнат и чатов, ожидающие заявки; заявки отклоняются без ответившего.</summary>
    public async Task<ResponseInfo<RoleAssignmentDeleteByUserResponse>> DeleteByUserAsync(RoleAssignmentDeleteByUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(RoleAssignmentCrudTexts.Messages.Start.DeletingByUser);

        try
        {
            var primaryErrors = RoleAssignmentCrudValidators.ValidatePrimaryDeleteByUserRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError, errors);
                return ResponseInfo.BadRequest<RoleAssignmentDeleteByUserResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var target = await GetUserWithDeletedAsync(request.UserId, cancellationToken);
            var domainErrors = RoleAssignmentCrudValidators.ValidateDomainDeleteByUser(target);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError, errors);
                return ResponseInfo.Invalid<RoleAssignmentDeleteByUserResponse>(MessageType.INVALID, messageText);
            }

            var participants = await unitOfWork.Participants.GetListAsync(
                ParticipantMapper.ToQueryOptions(userId: request.UserId),
                cancellationToken);
            List<ParticipantDeleteStageResponse> leftParticipants = [];
            foreach (var participant in participants)
            {
                var staged = await serviceParticipant.DeleteStageAsync(ParticipantMapper.ToDeleteStageRequest(participant.Id), cancellationToken);
                if (staged.MessageInfo.MessageType != MessageType.SAVED)
                {
                    return ResponseInfo.Error<RoleAssignmentDeleteByUserResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
                }

                leftParticipants.Add(staged.Response!);
            }

            var roomAdminRoles = await unitOfWork.RoomRoleAssignments.GetListAsync(
                RoleAssignmentMapper.ToRoomRoleQueryOptions(userId: request.UserId, roleName: RoleNames.RoomAdmin),
                cancellationToken);
            foreach (var roomAdminRole in roomAdminRoles)
            {
                var stageRequest = RoomRoleAssignmentMapper.ToRevokeRoomAdminStageRequest(roomAdminRole.RoomId, request.UserId);
                var staged = await serviceRoom.RevokeRoomAdminStageAsync(stageRequest, cancellationToken);
                if (staged.MessageInfo.MessageType != MessageType.SAVED)
                {
                    return ResponseInfo.Error<RoleAssignmentDeleteByUserResponse>(staged.MessageInfo.MessageType, staged.MessageInfo.MessageText);
                }
            }

            var pendingOptions = JoinRequestMapper.ToQueryOptions(userId: request.UserId, status: JoinRequestStatus.Pending);
            var pendingRequests = await unitOfWork.JoinRequests.GetListAsync(pendingOptions, cancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var pending in pendingRequests)
            {
                pending.Status = JoinRequestStatus.Rejected;
                pending.RespondedAt = now;
                await unitOfWork.JoinRequests.UpdateAsync(pending, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var left in leftParticipants)
            {
                await chatNotifyPort.NotifyParticipantLeftAsync(left.ChatId, left.Id, left.UserId, cancellationToken);
            }

            foreach (var rejected in pendingRequests)
            {
                var pendingCount = await unitOfWork.JoinRequests.CountAsync(
                    JoinRequestMapper.ToQueryOptions(chatId: rejected.ChatId, status: JoinRequestStatus.Pending),
                    cancellationToken);
                var changedEvent = JoinRequestMapper.ToChangedEvent(
                    JoinRequestChangedKind.Rejected,
                    rejected.ChatId,
                    rejected.Id,
                    rejected.UserId,
                    pendingCount);
                await domainEventPublisher.PublishAsync(changedEvent, cancellationToken);
            }

            var response = RoleAssignmentMapper.ToDeleteByUserResponse();
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Success.DeleteByUserCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, RoleAssignmentCrudTexts.Messages.Success.DeleteByUserCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(RoleAssignmentCrudTexts.Messages.Canceled.DeleteByUser);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError);
            return ResponseInfo.Error<RoleAssignmentDeleteByUserResponse>(MessageType.ERROR, RoleAssignmentCrudTexts.Messages.Error.DeleteByUserError);
        }
    }

    private async Task<UserPortModel?> GetUserWithDeletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = await userPort.GetListAsync([userId], cancellationToken);
        return users.GetValueOrDefault(userId);
    }

    private async Task<Dictionary<Guid, int>> GetRoomAdminCountsAsync(CancellationToken cancellationToken)
    {
        var roomAdmins = await unitOfWork.RoomRoleAssignments.GetListAsync(
            RoleAssignmentMapper.ToRoomRoleQueryOptions(roleName: RoleNames.RoomAdmin),
            cancellationToken);
        return roomAdmins.GroupBy(assignment => assignment.RoomId).ToDictionary(group => group.Key, group => group.Count());
    }

    private async Task<Dictionary<Guid, int>> GetChatAdminCountsAsync(CancellationToken cancellationToken)
    {
        var chatAdmins = await unitOfWork.ChatRoleAssignments.GetListAsync(
            RoleAssignmentMapper.ToChatRoleQueryOptions(roleName: RoleNames.ChatAdmin),
            cancellationToken);
        return chatAdmins.GroupBy(assignment => assignment.ChatId).ToDictionary(group => group.Key, group => group.Count());
    }
}
