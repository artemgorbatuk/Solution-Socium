using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Services.Security.Permissions.Catalogs;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Mapping;
using Services.Socium.Models;
using Services.Socium.Policies;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

public interface IServiceParticipant
{
    Task<ResponseInfo<ParticipantCreateResponse>> CreateAsync(ParticipantCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ParticipantCreateResponse>> CreateStageAsync(ParticipantCreateStageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ParticipantUpdateResponse>> UpdateAsync(ParticipantUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ParticipantDeletePageResponse>> DisplayDeletePageAsync(ParticipantDeletePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ParticipantDeleteResponse>> DeleteAsync(ParticipantDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ParticipantListPageResponse>> DisplayListPageAsync(ParticipantListPageRequest request, CancellationToken cancellationToken = default);
}

public class ServiceParticipant : IServiceParticipant
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IChatNotifyPort chatNotifyPort;
    private readonly IParticipantPolicy participantPolicy;
    private readonly ILogger<ServiceParticipant> logger;

    public ServiceParticipant(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IChatNotifyPort chatNotifyPort,
        IParticipantPolicy participantPolicy,
        ILogger<ServiceParticipant> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.chatNotifyPort = chatNotifyPort;
        this.participantPolicy = participantPolicy;
        this.logger = logger;
    }

    public async Task<ResponseInfo<ParticipantCreateResponse>> CreateAsync(ParticipantCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<ParticipantCreateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<ParticipantCreateResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<ParticipantCreateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canGrant = await participantPolicy.CanGrantParticipantAsync(
                user!.Id,
                chat!.RoomId,
                chat.Id,
                cancellationToken);
            var grantErrors = ParticipantCrudValidators.ValidateAccessGrant(canGrant);
            if (grantErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, grantErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<ParticipantCreateResponse>(MessageType.FORBIDDEN, messageText);
            }

            var targetUser = await userPort.GetSingleOrDefaultAsync(request.UserId, cancellationToken);
            var targetUserErrors = ParticipantCrudValidators.ValidateAccessibilityUser(targetUser);
            if (targetUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, targetUserErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<ParticipantCreateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = ParticipantMapper.ToQueryOptions(chatId: request.ChatId, userId: request.UserId);
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var existingParticipant = participants.SingleOrDefault();
            var domainErrors = ParticipantCrudValidators.ValidateDomainCreateRequest(existingParticipant);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<ParticipantCreateResponse>(MessageType.INVALID, messageText);
            }

            var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(
                request.ChatId,
                RoleNames.ChatAdmin,
                cancellationToken);
            var becameAdmin = adminCount == 0;

            var stageRequest = ParticipantMapper.ToCreateStageRequest(request.ChatId, request.UserId, becameAdmin);
            var staged = await CreateStageAsync(stageRequest, cancellationToken);
            if (staged.MessageInfo.MessageType != MessageType.SAVED)
            {
                return staged;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var joinedNotify = ParticipantMapper.ToNotifyParticipant(
                request.ChatId,
                staged.Response!.Id,
                targetUser!.Id,
                targetUser.Login,
                targetUser.Name,
                becameAdmin);
            await chatNotifyPort.NotifyParticipantJoinedAsync(joinedNotify, cancellationToken);

            logger.LogInformation(ParticipantCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success(staged.Response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<ParticipantCreateResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<ParticipantCreateResponse>> CreateStageAsync(
        ParticipantCreateStageRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.CreateStaging);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryCreateStageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.CreateStageError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.CreateStageError, errors);
                return ResponseInfo.BadRequest<ParticipantCreateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var newOptions = ParticipantMapper.ToGetNewOptions(request.ChatId, request.UserId);
            var model = unitOfWork.Participants.GetNew(newOptions);
            await unitOfWork.Participants.CreateAsync(model, cancellationToken);

            var participantAssignment = unitOfWork.ChatRoleAssignments.GetNew();
            ChatRoleAssignmentMapper.Apply(participantAssignment, request.UserId, model.ChatId, RoleNames.ChatParticipant);
            await unitOfWork.ChatRoleAssignments.CreateAsync(participantAssignment, cancellationToken);

            if (request.AssignChatAdmin)
            {
                var adminAssignment = unitOfWork.ChatRoleAssignments.GetNew();
                ChatRoleAssignmentMapper.Apply(adminAssignment, request.UserId, model.ChatId, RoleNames.ChatAdmin);
                await unitOfWork.ChatRoleAssignments.CreateAsync(adminAssignment, cancellationToken);
            }

            var response = ParticipantMapper.ToCreateResponse(model);
            logger.LogInformation(ParticipantCrudTexts.Messages.Success.CreateStageCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.CreateStageCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.CreateStage);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.CreateStageError);
            return ResponseInfo.Error<ParticipantCreateResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.CreateStageError);
        }
    }

    public async Task<ResponseInfo<ParticipantUpdateResponse>> UpdateAsync(ParticipantUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.Updating);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.BadRequest<ParticipantUpdateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Unauthorized<ParticipantUpdateResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Participants.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityParticipant(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<ParticipantUpdateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(model!.ChatId, cancellationToken);
            var chatAccessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
            if (chatAccessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, chatAccessibilityErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<ParticipantUpdateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canChangeRole = request.IsAdmin
                ? await participantPolicy.CanGrantAdminAsync(user!.Id, chat!.RoomId, model.ChatId, cancellationToken)
                : await participantPolicy.CanRevokeAdminAsync(user!.Id, chat!.RoomId, model.ChatId, cancellationToken);
            var accessErrors = ParticipantCrudValidators.ValidateAccessChangeRole(canChangeRole);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Forbidden<ParticipantUpdateResponse>(MessageType.FORBIDDEN, messageText);
            }

            var adminAssignment = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(
                model.UserId,
                model.ChatId,
                RoleNames.ChatAdmin,
                cancellationToken);
            var targetIsAdmin = adminAssignment is not null;
            var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(
                model.ChatId,
                RoleNames.ChatAdmin,
                cancellationToken);
            var domainErrors = ParticipantCrudValidators.ValidateDomainUpdateRequest(request, targetIsAdmin, adminCount);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<ParticipantUpdateResponse>(MessageType.INVALID, messageText);
            }

            if (request.IsAdmin)
            {
                if (adminAssignment is null)
                {
                    var created = unitOfWork.ChatRoleAssignments.GetNew();
                    ChatRoleAssignmentMapper.Apply(created, model.UserId, model.ChatId, RoleNames.ChatAdmin);
                    await unitOfWork.ChatRoleAssignments.CreateAsync(created, cancellationToken);
                }
            }
            else if (adminAssignment is not null)
            {
                await unitOfWork.ChatRoleAssignments.DeleteAsync(adminAssignment, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var targetUser = await userPort.GetSingleOrDefaultAsync(model.UserId, cancellationToken);
            var roleChangedNotify = ParticipantMapper.ToNotifyParticipant(
                model.ChatId,
                model.Id,
                model.UserId,
                targetUser?.Login ?? string.Empty,
                targetUser?.Name ?? string.Empty,
                request.IsAdmin);
            await chatNotifyPort.NotifyParticipantRoleChangedAsync(roleChangedNotify, cancellationToken);

            var response = ParticipantMapper.ToUpdateResponse(model, request.IsAdmin);
            logger.LogInformation(ParticipantCrudTexts.Messages.Success.UpdateCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.UpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.Update);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.UpdateError);
            return ResponseInfo.Error<ParticipantUpdateResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.UpdateError);
        }
    }

    public async Task<ResponseInfo<ParticipantDeletePageResponse>> DisplayDeletePageAsync(ParticipantDeletePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.DisplayDeleting);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryDeletePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.BadRequest<ParticipantDeletePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Unauthorized<ParticipantDeletePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<ParticipantDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canLeave = await participantPolicy.CanLeaveAsync(user!.Id, chat!.RoomId, chat.Id, cancellationToken);
            var accessErrors = ParticipantCrudValidators.ValidateAccessLeave(canLeave);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<ParticipantDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var participantOptions = ParticipantMapper.ToQueryOptions(chatId: request.ChatId);
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var currentParticipant = participants.SingleOrDefault(participant => participant.UserId == user.Id);
            ICollection<string> leaveErrors;
            if (currentParticipant is null)
            {
                leaveErrors = [];
            }
            else
            {
                var currentIsAdmin = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(
                    currentParticipant.UserId,
                    currentParticipant.ChatId,
                    RoleNames.ChatAdmin,
                    cancellationToken) is not null;
                var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(
                    request.ChatId,
                    RoleNames.ChatAdmin,
                    cancellationToken);
                leaveErrors = [.. ParticipantCrudValidators.ValidateDomainDelete(currentIsAdmin, participants.Count(), adminCount)];
            }

            var response = ParticipantMapper.ToDeletePageResponse(request.ChatId, leaveErrors);
            logger.LogInformation(ParticipantCrudTexts.Messages.Success.DisplayDeleteCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ParticipantCrudTexts.Messages.Success.DisplayDeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.DisplayDelete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.DisplayDeleteError);
            return ResponseInfo.Error<ParticipantDeletePageResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.DisplayDeleteError);
        }
    }

    public async Task<ResponseInfo<ParticipantDeleteResponse>> DeleteAsync(ParticipantDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var isKick = request.Id is Guid participantId && participantId != Guid.Empty;
        logger.LogInformation(isKick
            ? ParticipantCrudTexts.Messages.Start.Kicking
            : ParticipantCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var errorPrefix = isKick
                    ? ParticipantCrudTexts.Messages.Error.KickError
                    : ParticipantCrudTexts.Messages.Error.DeleteError;
                var messageText = $"{errorPrefix}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", errorPrefix, errors);
                return ResponseInfo.BadRequest<ParticipantDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            return isKick
                ? await KickAsync(request.Id!.Value, cancellationToken)
                : await LeaveAsync(request.ChatId!.Value, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(isKick
                ? ParticipantCrudTexts.Messages.Canceled.Kick
                : ParticipantCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            var errorPrefix = isKick
                ? ParticipantCrudTexts.Messages.Error.KickError
                : ParticipantCrudTexts.Messages.Error.DeleteError;
            logger.LogError(exception, "{Message}", errorPrefix);
            return ResponseInfo.Error<ParticipantDeleteResponse>(MessageType.ERROR, errorPrefix);
        }
    }

    private async Task<ResponseInfo<ParticipantDeleteResponse>> LeaveAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is Guid currentUserId
            ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
            : null;
        var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
        if (currentUserErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, currentUserErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Unauthorized<ParticipantDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
        }

        var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(chatId, cancellationToken);
        var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
        if (accessibilityErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, accessibilityErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.NotFound<ParticipantDeleteResponse>(MessageType.NOT_FOUND, messageText);
        }

        var canLeave = await participantPolicy.CanLeaveAsync(user!.Id, chat!.RoomId, chat.Id, cancellationToken);
        var accessErrors = ParticipantCrudValidators.ValidateAccessLeave(canLeave);
        if (accessErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, accessErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Forbidden<ParticipantDeleteResponse>(MessageType.FORBIDDEN, messageText);
        }

        var participantOptions = ParticipantMapper.ToQueryOptions(chatId: chatId);
        var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
        var currentParticipant = participants.SingleOrDefault(participant => participant.UserId == user.Id);
        if (currentParticipant is null)
        {
            var messageText =
                $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{ParticipantCrudTexts.Messages.Validation.NotParticipant}";
            logger.LogError("{Message}", messageText);
            return ResponseInfo.Forbidden<ParticipantDeleteResponse>(MessageType.FORBIDDEN, messageText);
        }

        var currentIsAdmin = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(
            currentParticipant.UserId,
            currentParticipant.ChatId,
            RoleNames.ChatAdmin,
            cancellationToken) is not null;
        var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(
            chatId,
            RoleNames.ChatAdmin,
            cancellationToken);
        var domainErrors = ParticipantCrudValidators.ValidateDomainDelete(
            currentIsAdmin,
            participants.Count(),
            adminCount);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DeleteError, errors);
            return ResponseInfo.Invalid<ParticipantDeleteResponse>(MessageType.INVALID, messageText);
        }

        await RemoveParticipantAsync(currentParticipant.Id, cancellationToken);

        var response = ParticipantMapper.ToDeleteResponse();
        logger.LogInformation(ParticipantCrudTexts.Messages.Success.DeleteCompleted);
        return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.DeleteCompleted);
    }

    private async Task<ResponseInfo<ParticipantDeleteResponse>> KickAsync(Guid participantId, CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is Guid currentUserId
            ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
            : null;
        var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
        if (currentUserErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, currentUserErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.KickError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.KickError, errors);
            return ResponseInfo.Unauthorized<ParticipantDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
        }

        var model = await unitOfWork.Participants.GetSingleOrDefaultAsync(participantId, cancellationToken);
        var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityParticipant(model);
        if (accessibilityErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, accessibilityErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.KickError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.KickError, errors);
            return ResponseInfo.NotFound<ParticipantDeleteResponse>(MessageType.NOT_FOUND, messageText);
        }

        var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(model!.ChatId, cancellationToken);
        var chatAccessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
        if (chatAccessibilityErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, chatAccessibilityErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.KickError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.KickError, errors);
            return ResponseInfo.NotFound<ParticipantDeleteResponse>(MessageType.NOT_FOUND, messageText);
        }

        var canKick = await participantPolicy.CanRevokeParticipantAsync(user!.Id, chat!.RoomId, model.ChatId, cancellationToken);
        var accessErrors = ParticipantCrudValidators.ValidateAccessKick(canKick);
        if (accessErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, accessErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.KickError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.KickError, errors);
            return ResponseInfo.Forbidden<ParticipantDeleteResponse>(MessageType.FORBIDDEN, messageText);
        }

        var targetIsAdmin = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(
            model.UserId,
            model.ChatId,
            RoleNames.ChatAdmin,
            cancellationToken) is not null;
        var adminCount = await unitOfWork.ChatRoleAssignments.CountByChatAndRoleAsync(
            model.ChatId,
            RoleNames.ChatAdmin,
            cancellationToken);
        var domainErrors = ParticipantCrudValidators.ValidateDomainKick(
            isSelf: model.UserId == user.Id,
            targetIsAdmin,
            adminCount);
        if (domainErrors.Any())
        {
            var errors = string.Join(Environment.NewLine, domainErrors);
            var messageText = $"{ParticipantCrudTexts.Messages.Error.KickError}{Environment.NewLine}{errors}";
            logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.KickError, errors);
            return ResponseInfo.Invalid<ParticipantDeleteResponse>(MessageType.INVALID, messageText);
        }

        await RemoveParticipantAsync(model.Id, cancellationToken);

        var response = ParticipantMapper.ToDeleteResponse();
        logger.LogInformation(ParticipantCrudTexts.Messages.Success.KickCompleted);
        return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.KickCompleted);
    }

    private async Task RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken)
    {
        var model = await unitOfWork.Participants.GetSingleOrDefaultAsync(participantId, cancellationToken);
        var leftChatId = model!.ChatId;
        var leftParticipantId = model.Id;
        var leftUserId = model.UserId;

        var roleNames = await unitOfWork.ChatRoleAssignments.GetRoleNamesAsync(leftUserId, leftChatId, cancellationToken);
        foreach (var roleName in roleNames)
        {
            var assignment = await unitOfWork.ChatRoleAssignments.GetSingleOrDefaultAsync(
                leftUserId,
                leftChatId,
                roleName,
                cancellationToken);
            if (assignment is not null)
            {
                await unitOfWork.ChatRoleAssignments.DeleteAsync(assignment, cancellationToken);
            }
        }

        await unitOfWork.Participants.DeleteAsync(model, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await chatNotifyPort.NotifyParticipantLeftAsync(leftChatId, leftParticipantId, leftUserId, cancellationToken);
    }

    public async Task<ResponseInfo<ParticipantListPageResponse>> DisplayListPageAsync(ParticipantListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<ParticipantListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = ParticipantCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<ParticipantListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = ParticipantCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<ParticipantListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRead = await participantPolicy.CanReadAsync(user!.Id, chat!.RoomId, chat.Id, cancellationToken);
            var accessErrors = ParticipantCrudValidators.ValidateAccessRead(canRead);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Forbidden<ParticipantListPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var participantOptions = ParticipantMapper.ToQueryOptions(chatId: request.ChatId);
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var adminUserIds = (await unitOfWork.ChatRoleAssignments.GetUserIdsByChatAndRoleAsync(
                    request.ChatId,
                    RoleNames.ChatAdmin,
                    cancellationToken))
                .ToHashSet();
            var users = await userPort.GetListAsync(participants.Select(participant => participant.UserId), cancellationToken);
            ICollection<ParticipantListModel> rows =
            [
                .. participants
                    .Select(participant => ParticipantMapper.ToListModel(
                        participant,
                        users.GetValueOrDefault(participant.UserId),
                        adminUserIds.Contains(participant.UserId)))
                    .OrderBy(row => row.Name)
                    .ThenBy(row => row.Login)
                    .ThenBy(row => row.Id)
            ];
            var canChangeAdmin = await participantPolicy.CanGrantAdminAsync(user.Id, chat.RoomId, chat.Id, cancellationToken);
            var canGrantParticipant = await participantPolicy.CanGrantParticipantAsync(
                user.Id,
                chat.RoomId,
                chat.Id,
                cancellationToken);
            var canKick = await participantPolicy.CanRevokeParticipantAsync(user.Id, chat.RoomId, chat.Id, cancellationToken);
            var response = ParticipantMapper.ToListPageResponse(rows, canChangeAdmin, canGrantParticipant, canKick);

            logger.LogInformation(ParticipantCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, ParticipantCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<ParticipantListPageResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.DisplayListError);
        }
    }
}
