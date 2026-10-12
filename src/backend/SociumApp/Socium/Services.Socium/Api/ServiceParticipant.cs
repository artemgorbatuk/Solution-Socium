using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var currentParticipant = participants.SingleOrDefault(participant => participant.UserId == user!.Id);
            var domainErrors = ParticipantCrudValidators.ValidateDomainCreateRequest(currentParticipant);
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

            var newOptions = ParticipantMapper.ToGetNewOptions(request.ChatId, user!.Id);
            var model = unitOfWork.Participants.GetNew(newOptions);
            await unitOfWork.Participants.CreateAsync(model, cancellationToken);

            var participantAssignment = unitOfWork.ChatRoleAssignments.GetNew();
            ChatRoleAssignmentMapper.Apply(participantAssignment, user.Id, model.ChatId, RoleNames.ChatParticipant);
            await unitOfWork.ChatRoleAssignments.CreateAsync(participantAssignment, cancellationToken);

            if (becameAdmin)
            {
                var adminAssignment = unitOfWork.ChatRoleAssignments.GetNew();
                ChatRoleAssignmentMapper.Apply(adminAssignment, user.Id, model.ChatId, RoleNames.ChatAdmin);
                await unitOfWork.ChatRoleAssignments.CreateAsync(adminAssignment, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await chatNotifyPort.NotifyParticipantJoinedAsync(
                new ChatNotifyParticipantModel
                {
                    ChatId = model.ChatId,
                    Id = model.Id,
                    UserId = user.Id,
                    Login = user.Login,
                    Name = user.Name,
                    IsAdmin = becameAdmin,
                },
                cancellationToken);

            var response = ParticipantMapper.ToCreateResponse(model);
            logger.LogInformation(ParticipantCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.CreateCompleted);
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
            await chatNotifyPort.NotifyParticipantRoleChangedAsync(
                new ChatNotifyParticipantModel
                {
                    ChatId = model.ChatId,
                    Id = model.Id,
                    UserId = model.UserId,
                    Login = targetUser?.Login ?? string.Empty,
                    Name = targetUser?.Name ?? string.Empty,
                    IsAdmin = request.IsAdmin,
                },
                cancellationToken);

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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
            };
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
        logger.LogInformation(ParticipantCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = ParticipantCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{ParticipantCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ParticipantCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<ParticipantDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

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

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
            };
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
                request.ChatId,
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

            var model = await unitOfWork.Participants.GetSingleOrDefaultAsync(currentParticipant.Id, cancellationToken);
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

            var response = new ParticipantDeleteResponse { IsDeleted = true };
            logger.LogInformation(ParticipantCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, ParticipantCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(ParticipantCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", ParticipantCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<ParticipantDeleteResponse>(MessageType.ERROR, ParticipantCrudTexts.Messages.Error.DeleteError);
        }
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
            };
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
            var canChangeRole = await participantPolicy.CanGrantAdminAsync(user.Id, chat.RoomId, chat.Id, cancellationToken);
            var response = new ParticipantListPageResponse
            {
                Rows = rows,
                RowExists = rows.Count > 0,
                RowCount = rows.Count,
                CanChangeRole = canChangeRole,
            };

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
