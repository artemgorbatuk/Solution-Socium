using Datasource.Socium.Ef.Enums;
using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
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

public interface IServiceJoinRequest
{
    Task<ResponseInfo<JoinRequestCreateResponse>> CreateAsync(
        JoinRequestCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseInfo<JoinRequestAcceptResponse>> AcceptAsync(
        JoinRequestAcceptRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseInfo<JoinRequestRejectResponse>> RejectAsync(
        JoinRequestRejectRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseInfo<JoinRequestPendingListPageResponse>> DisplayPendingListPageAsync(
        JoinRequestPendingListPageRequest request,
        CancellationToken cancellationToken = default);
}

public class ServiceJoinRequest : IServiceJoinRequest
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IChatPolicy chatPolicy;
    private readonly IParticipantPolicy participantPolicy;
    private readonly IServiceParticipant serviceParticipant;
    private readonly IChatNotifyPort chatNotifyPort;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceJoinRequest> logger;

    public ServiceJoinRequest(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IChatPolicy chatPolicy,
        IParticipantPolicy participantPolicy,
        IServiceParticipant serviceParticipant,
        IChatNotifyPort chatNotifyPort,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider,
        ILogger<ServiceJoinRequest> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.chatPolicy = chatPolicy;
        this.participantPolicy = participantPolicy;
        this.serviceParticipant = serviceParticipant;
        this.chatNotifyPort = chatNotifyPort;
        this.domainEventPublisher = domainEventPublisher;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ResponseInfo<JoinRequestCreateResponse>> CreateAsync(
        JoinRequestCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(JoinRequestCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = JoinRequestCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<JoinRequestCreateResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = JoinRequestCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<JoinRequestCreateResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = JoinRequestCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<JoinRequestCreateResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canRead = await chatPolicy.CanReadAsync(user!.Id, chat!.RoomId, chat.Id, cancellationToken);
            var accessErrors = JoinRequestCrudValidators.ValidateAccess(canRead);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<JoinRequestCreateResponse>(MessageType.FORBIDDEN, messageText);
            }

            var participantOptions = ParticipantMapper.ToQueryOptions(chatId: request.ChatId, userId: user.Id);
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var currentParticipant = participants.SingleOrDefault();

            var pendingOptions = JoinRequestMapper.ToQueryOptions(
                chatId: request.ChatId,
                userId: user.Id,
                status: JoinRequestStatus.Pending);
            var pendingList = await unitOfWork.JoinRequests.GetListAsync(pendingOptions, cancellationToken);
            var pending = pendingList.SingleOrDefault();

            var domainErrors = JoinRequestCrudValidators.ValidateDomainCreate(currentParticipant, pending);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<JoinRequestCreateResponse>(MessageType.INVALID, messageText);
            }

            var newOptions = JoinRequestMapper.ToGetNewOptions(request.ChatId, user.Id, user.Id);
            var model = unitOfWork.JoinRequests.GetNew(newOptions);
            await unitOfWork.JoinRequests.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var pendingCount = await unitOfWork.JoinRequests.CountAsync(
                JoinRequestMapper.ToQueryOptions(chatId: request.ChatId, status: JoinRequestStatus.Pending),
                cancellationToken);
            var changedEvent = JoinRequestMapper.ToChangedEvent(
                JoinRequestChangedKind.Created,
                request.ChatId,
                model.Id,
                user.Id,
                pendingCount);
            await domainEventPublisher.PublishAsync(changedEvent, cancellationToken);

            var response = JoinRequestMapper.ToCreateResponse(model);
            logger.LogInformation(JoinRequestCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, JoinRequestCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(JoinRequestCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", JoinRequestCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<JoinRequestCreateResponse>(MessageType.ERROR, JoinRequestCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<JoinRequestAcceptResponse>> AcceptAsync(
        JoinRequestAcceptRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(JoinRequestCrudTexts.Messages.Start.Accepting);

        try
        {
            var primaryErrors = JoinRequestCrudValidators.ValidatePrimaryAcceptRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.BadRequest<JoinRequestAcceptResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = JoinRequestCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.Unauthorized<JoinRequestAcceptResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var joinRequest = await unitOfWork.JoinRequests.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = JoinRequestCrudValidators.ValidateAccessibilityJoinRequest(joinRequest);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.NotFound<JoinRequestAcceptResponse>(MessageType.NOT_FOUND, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(joinRequest!.ChatId, cancellationToken);
            var chatErrors = JoinRequestCrudValidators.ValidateAccessibilityChat(chat);
            if (chatErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, chatErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.NotFound<JoinRequestAcceptResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canGrant = await participantPolicy.CanGrantParticipantAsync(
                user!.Id,
                chat!.RoomId,
                chat.Id,
                cancellationToken);
            var accessErrors = JoinRequestCrudValidators.ValidateAccess(canGrant);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.Forbidden<JoinRequestAcceptResponse>(MessageType.FORBIDDEN, messageText);
            }

            var participantOptions = ParticipantMapper.ToQueryOptions(chatId: joinRequest.ChatId, userId: joinRequest.UserId);
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var targetParticipant = participants.SingleOrDefault();
            var domainErrors = JoinRequestCrudValidators.ValidateDomainRespond(joinRequest, targetParticipant);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.AcceptError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.AcceptError, errors);
                return ResponseInfo.Invalid<JoinRequestAcceptResponse>(MessageType.INVALID, messageText);
            }

            var targetUser = await userPort.GetSingleOrDefaultAsync(joinRequest.UserId, cancellationToken);
            var stageRequest = ParticipantMapper.ToCreateStageRequest(joinRequest.ChatId, joinRequest.UserId, assignChatAdmin: false);
            var staged = await serviceParticipant.CreateStageAsync(stageRequest, cancellationToken);
            if (staged.MessageInfo.MessageType != MessageType.SAVED)
            {
                return ResponseInfo.Error<JoinRequestAcceptResponse>(
                    staged.MessageInfo.MessageType,
                    staged.MessageInfo.MessageText ?? JoinRequestCrudTexts.Messages.Error.AcceptError);
            }

            joinRequest.Status = JoinRequestStatus.Accepted;
            joinRequest.RespondedByUserId = user.Id;
            joinRequest.RespondedAt = timeProvider.GetUtcNow().UtcDateTime;
            await unitOfWork.JoinRequests.UpdateAsync(joinRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var joinedNotify = ParticipantMapper.ToNotifyParticipant(
                joinRequest.ChatId,
                staged.Response!.Id,
                joinRequest.UserId,
                targetUser?.Login ?? string.Empty,
                targetUser?.Name ?? UserPortTexts.DeletedUserName,
                isAdmin: false);
            await chatNotifyPort.NotifyParticipantJoinedAsync(joinedNotify, cancellationToken);

            var pendingCount = await unitOfWork.JoinRequests.CountAsync(
                JoinRequestMapper.ToQueryOptions(chatId: joinRequest.ChatId, status: JoinRequestStatus.Pending),
                cancellationToken);
            var changedEvent = JoinRequestMapper.ToChangedEvent(
                JoinRequestChangedKind.Accepted,
                joinRequest.ChatId,
                joinRequest.Id,
                joinRequest.UserId,
                pendingCount);
            await domainEventPublisher.PublishAsync(changedEvent, cancellationToken);

            var response = JoinRequestMapper.ToAcceptResponse(joinRequest, staged.Response.Id);
            logger.LogInformation(JoinRequestCrudTexts.Messages.Success.AcceptCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, JoinRequestCrudTexts.Messages.Success.AcceptCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(JoinRequestCrudTexts.Messages.Canceled.Accept);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", JoinRequestCrudTexts.Messages.Error.AcceptError);
            return ResponseInfo.Error<JoinRequestAcceptResponse>(MessageType.ERROR, JoinRequestCrudTexts.Messages.Error.AcceptError);
        }
    }

    public async Task<ResponseInfo<JoinRequestRejectResponse>> RejectAsync(
        JoinRequestRejectRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(JoinRequestCrudTexts.Messages.Start.Rejecting);

        try
        {
            var primaryErrors = JoinRequestCrudValidators.ValidatePrimaryRejectRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.RejectError, errors);
                return ResponseInfo.BadRequest<JoinRequestRejectResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = JoinRequestCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.RejectError, errors);
                return ResponseInfo.Unauthorized<JoinRequestRejectResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var joinRequest = await unitOfWork.JoinRequests.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = JoinRequestCrudValidators.ValidateAccessibilityJoinRequest(joinRequest);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.RejectError, errors);
                return ResponseInfo.NotFound<JoinRequestRejectResponse>(MessageType.NOT_FOUND, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(joinRequest!.ChatId, cancellationToken);
            var chatErrors = JoinRequestCrudValidators.ValidateAccessibilityChat(chat);
            if (chatErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, chatErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.RejectError, errors);
                return ResponseInfo.NotFound<JoinRequestRejectResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canGrant = await participantPolicy.CanGrantParticipantAsync(
                user!.Id,
                chat!.RoomId,
                chat.Id,
                cancellationToken);
            var accessErrors = JoinRequestCrudValidators.ValidateAccess(canGrant);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.RejectError, errors);
                return ResponseInfo.Forbidden<JoinRequestRejectResponse>(MessageType.FORBIDDEN, messageText);
            }

            if (joinRequest.Status != JoinRequestStatus.Pending)
            {
                var messageText =
                    $"{JoinRequestCrudTexts.Messages.Error.RejectError}{Environment.NewLine}{JoinRequestCrudTexts.Messages.Validation.NotPending}";
                logger.LogError("{Message}", messageText);
                return ResponseInfo.Invalid<JoinRequestRejectResponse>(MessageType.INVALID, messageText);
            }

            joinRequest.Status = JoinRequestStatus.Rejected;
            joinRequest.RespondedByUserId = user.Id;
            joinRequest.RespondedAt = timeProvider.GetUtcNow().UtcDateTime;
            await unitOfWork.JoinRequests.UpdateAsync(joinRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var pendingCount = await unitOfWork.JoinRequests.CountAsync(
                JoinRequestMapper.ToQueryOptions(chatId: joinRequest.ChatId, status: JoinRequestStatus.Pending),
                cancellationToken);
            var changedEvent = JoinRequestMapper.ToChangedEvent(
                JoinRequestChangedKind.Rejected,
                joinRequest.ChatId,
                joinRequest.Id,
                joinRequest.UserId,
                pendingCount);
            await domainEventPublisher.PublishAsync(changedEvent, cancellationToken);

            var response = JoinRequestMapper.ToRejectResponse(joinRequest);
            logger.LogInformation(JoinRequestCrudTexts.Messages.Success.RejectCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, JoinRequestCrudTexts.Messages.Success.RejectCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(JoinRequestCrudTexts.Messages.Canceled.Reject);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", JoinRequestCrudTexts.Messages.Error.RejectError);
            return ResponseInfo.Error<JoinRequestRejectResponse>(MessageType.ERROR, JoinRequestCrudTexts.Messages.Error.RejectError);
        }
    }

    public async Task<ResponseInfo<JoinRequestPendingListPageResponse>> DisplayPendingListPageAsync(
        JoinRequestPendingListPageRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(JoinRequestCrudTexts.Messages.Start.DisplayPendingListLoading);

        try
        {
            var primaryErrors = JoinRequestCrudValidators.ValidatePrimaryPendingListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.DisplayPendingListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.DisplayPendingListError, errors);
                return ResponseInfo.BadRequest<JoinRequestPendingListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = JoinRequestCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.DisplayPendingListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.DisplayPendingListError, errors);
                return ResponseInfo.Unauthorized<JoinRequestPendingListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = JoinRequestCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.DisplayPendingListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.DisplayPendingListError, errors);
                return ResponseInfo.NotFound<JoinRequestPendingListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var canGrant = await participantPolicy.CanGrantParticipantAsync(
                user!.Id,
                chat!.RoomId,
                chat.Id,
                cancellationToken);
            var accessErrors = JoinRequestCrudValidators.ValidateAccess(canGrant);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{JoinRequestCrudTexts.Messages.Error.DisplayPendingListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", JoinRequestCrudTexts.Messages.Error.DisplayPendingListError, errors);
                return ResponseInfo.Forbidden<JoinRequestPendingListPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var pendingOptions = JoinRequestMapper.ToQueryOptions(chatId: request.ChatId, status: JoinRequestStatus.Pending);
            var pending = await unitOfWork.JoinRequests.GetListAsync(pendingOptions, cancellationToken);
            var userIds = pending.Select(item => item.UserId).Distinct().ToList();
            var usersById = await userPort.GetListAsync(userIds, cancellationToken);

            ICollection<JoinRequestPendingListModel> rows =
            [
                .. pending.Select(item =>
                {
                    usersById.TryGetValue(item.UserId, out var portUser);
                    return JoinRequestMapper.ToPendingListModel(item, portUser);
                }),
            ];

            var response = JoinRequestMapper.ToPendingListPageResponse(rows);
            logger.LogInformation(JoinRequestCrudTexts.Messages.Success.DisplayPendingListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, JoinRequestCrudTexts.Messages.Success.DisplayPendingListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(JoinRequestCrudTexts.Messages.Canceled.DisplayPendingList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", JoinRequestCrudTexts.Messages.Error.DisplayPendingListError);
            return ResponseInfo.Error<JoinRequestPendingListPageResponse>(
                MessageType.ERROR,
                JoinRequestCrudTexts.Messages.Error.DisplayPendingListError);
        }
    }
}
