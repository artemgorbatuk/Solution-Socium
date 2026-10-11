using Datasource.Socium.Ef.Models;
using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Mapping;
using Services.Socium.Models;
using Services.Socium.Normalizers;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

public interface IServiceMessage
{
    Task<ResponseInfo<MessageCreatePageResponse>> DisplayCreatePageAsync(MessageCreatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageCreatePageResponse>> CreateAsync(MessageCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageUpdatePageResponse>> DisplayUpdatePageAsync(MessageUpdatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageUpdatePageResponse>> UpdateAsync(MessageUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageDeletePageResponse>> DisplayDeletePageAsync(MessageDeletePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageDeleteResponse>> DeleteAsync(MessageDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageInfoPageResponse>> DisplayInfoPageAsync(MessageInfoPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageListPageResponse>> DisplayListPageAsync(MessageListPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageDeliveredResponse>> MarkDeliveredAsync(MessageDeliveredRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<MessageReadResponse>> MarkReadAsync(MessageReadRequest request, CancellationToken cancellationToken = default);
}

public class ServiceMessage : IServiceMessage
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IUserPort userPort;
    private readonly IFilePort filePort;
    private readonly IChatNotifyPort chatNotifyPort;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceMessage> logger;

    public ServiceMessage(
        IUnitOfWorkSocium unitOfWork,
        ICurrentUser currentUser,
        IUserPort userPort,
        IFilePort filePort,
        IChatNotifyPort chatNotifyPort,
        TimeProvider timeProvider,
        ILogger<ServiceMessage> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.userPort = userPort;
        this.filePort = filePort;
        this.chatNotifyPort = chatNotifyPort;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ResponseInfo<MessageCreatePageResponse>> DisplayCreatePageAsync(MessageCreatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.DisplayCreating);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryCreatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.BadRequest<MessageCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Unauthorized<MessageCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.NotFound<MessageCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessParticipant(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.Forbidden<MessageCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var newOptions = new MessageGetNewOptions
            {
                ChatId = request.ChatId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SenderUserId = user.Id,
            };
            var model = unitOfWork.Messages.GetNew(newOptions);
            var response = MessageMapper.ToCreatePageResponse(model);
            logger.LogInformation(MessageCrudTexts.Messages.Success.DisplayCreateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, MessageCrudTexts.Messages.Success.DisplayCreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.DisplayCreate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DisplayCreateError);
            return ResponseInfo.Error<MessageCreatePageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayCreateError);
        }
    }

    public async Task<ResponseInfo<MessageCreatePageResponse>> CreateAsync(MessageCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<MessageCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Unauthorized<MessageCreatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<MessageCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var senderParticipant = participants.SingleOrDefault(participant => participant.UserId == user!.Id);
            var accessErrors = MessageCrudValidators.ValidateAccessParticipant(senderParticipant);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Forbidden<MessageCreatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = MessageCrudValidators.ValidateDomainCreateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<MessageCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var textNormalized = MessageNormalizer.Text(request.Text);
            var fileIds = (request.FileIds ?? []).Distinct().ToList();
            var newOptions = new MessageGetNewOptions
            {
                ChatId = request.ChatId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SenderUserId = user!.Id,
                RecipientUserIds = [.. participants
                    .Where(participant => participant.UserId != user.Id)
                    .Select(participant => participant.UserId)],
            };
            var model = unitOfWork.Messages.GetNew(newOptions);
            MessageMapper.Apply(model, textNormalized);
            await unitOfWork.Messages.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (fileIds.Count > 0)
            {
                var attachResult = await filePort.AttachToMessageAsync(model.Id, fileIds, user.Id, cancellationToken);
                if (attachResult.MessageInfo.MessageType != MessageType.SAVED)
                {
                    await unitOfWork.Messages.DeleteAsync(model, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    var attachText = string.IsNullOrWhiteSpace(attachResult.MessageInfo.MessageText)
                        ? MessageCrudTexts.Messages.Validation.AttachmentsFailed
                        : attachResult.MessageInfo.MessageText;
                    var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{attachText}";
                    logger.LogError("{Message}: {Attach}", MessageCrudTexts.Messages.Error.CreateError, attachText);
                    return attachResult.MessageInfo.MessageType switch
                    {
                        MessageType.FORBIDDEN => ResponseInfo.Forbidden<MessageCreatePageResponse>(MessageType.FORBIDDEN, messageText),
                        MessageType.NOT_FOUND => ResponseInfo.NotFound<MessageCreatePageResponse>(MessageType.NOT_FOUND, messageText),
                        MessageType.INVALID => ResponseInfo.Invalid<MessageCreatePageResponse>(MessageType.INVALID, messageText),
                        MessageType.BAD_REQUEST => ResponseInfo.BadRequest<MessageCreatePageResponse>(MessageType.BAD_REQUEST, messageText),
                        _ => ResponseInfo.Error<MessageCreatePageResponse>(MessageType.ERROR, messageText),
                    };
                }
            }

            var createdAttachments = await filePort.GetByMessageIdsAsync([model.Id], cancellationToken);
            var createdAttachmentList = createdAttachments.GetValueOrDefault(model.Id) ?? [];
            await chatNotifyPort.NotifyMessageCreatedAsync(
                ToNotifyMessage(model, user.Name, createdAttachmentList),
                cancellationToken);
            var createdPreview = string.IsNullOrWhiteSpace(model.Text)
                ? (createdAttachmentList.Count > 0 ? "Вложение" : null)
                : ChatMapper.TruncatePreview(model.Text);
            await NotifyChatListForParticipantsAsync(
                model.ChatId,
                user.Id,
                createdPreview,
                model.CreatedAt,
                user.Name,
                createdAttachmentList.Count > 0,
                lastMessageCleared: false,
                cancellationToken);

            logger.LogInformation(MessageCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success<MessageCreatePageResponse>(MessageType.SAVED, MessageCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<MessageCreatePageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<MessageUpdatePageResponse>> DisplayUpdatePageAsync(MessageUpdatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.DisplayUpdating);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryUpdatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.BadRequest<MessageUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Unauthorized<MessageUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.NotFound<MessageUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessSender(participants.SingleOrDefault(), model);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Forbidden<MessageUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = MessageMapper.ToUpdatePageResponse(model);
            logger.LogInformation(MessageCrudTexts.Messages.Success.DisplayUpdateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, MessageCrudTexts.Messages.Success.DisplayUpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.DisplayUpdate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DisplayUpdateError);
            return ResponseInfo.Error<MessageUpdatePageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayUpdateError);
        }
    }

    public async Task<ResponseInfo<MessageUpdatePageResponse>> UpdateAsync(MessageUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.Updating);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.BadRequest<MessageUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Unauthorized<MessageUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<MessageUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessSender(participants.SingleOrDefault(), model);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Forbidden<MessageUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = MessageCrudValidators.ValidateDomainUpdateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<MessageUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            var textNormalized = MessageNormalizer.Text(request.Text);
            MessageMapper.Apply(model, textNormalized);
            await unitOfWork.Messages.UpdateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedAttachments = await filePort.GetByMessageIdsAsync([model.Id], cancellationToken);
            var updatedAttachmentList = updatedAttachments.GetValueOrDefault(model.Id) ?? [];
            await chatNotifyPort.NotifyMessageUpdatedAsync(
                ToNotifyMessage(model, user.Name, updatedAttachmentList),
                cancellationToken);
            var last = (await unitOfWork.Messages.GetLastByChatIdsAsync([model.ChatId], cancellationToken))
                .GetValueOrDefault(model.ChatId);
            if (last is not null && last.MessageId == model.Id)
            {
                var updatedPreview = string.IsNullOrWhiteSpace(model.Text)
                    ? (updatedAttachmentList.Count > 0 ? "Вложение" : null)
                    : ChatMapper.TruncatePreview(model.Text);
                await NotifyChatListForParticipantsAsync(
                    model.ChatId,
                    senderUserId: null,
                    updatedPreview,
                    model.CreatedAt,
                    user.Name,
                    updatedAttachmentList.Count > 0,
                    lastMessageCleared: false,
                    cancellationToken);
            }

            logger.LogInformation(MessageCrudTexts.Messages.Success.UpdateCompleted);
            return ResponseInfo.Success<MessageUpdatePageResponse>(MessageType.SAVED, MessageCrudTexts.Messages.Success.UpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.Update);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.UpdateError);
            return ResponseInfo.Error<MessageUpdatePageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.UpdateError);
        }
    }

    public async Task<ResponseInfo<MessageDeletePageResponse>> DisplayDeletePageAsync(MessageDeletePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.DisplayDeleting);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryDeletePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.BadRequest<MessageDeletePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Unauthorized<MessageDeletePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<MessageDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessSender(participants.SingleOrDefault(), model);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<MessageDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = MessageMapper.ToDeletePageResponse(model);
            logger.LogInformation(MessageCrudTexts.Messages.Success.DisplayDeleteCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, MessageCrudTexts.Messages.Success.DisplayDeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.DisplayDelete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DisplayDeleteError);
            return ResponseInfo.Error<MessageDeletePageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayDeleteError);
        }
    }

    public async Task<ResponseInfo<MessageDeleteResponse>> DeleteAsync(MessageDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<MessageDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<MessageDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<MessageDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessSender(participants.SingleOrDefault(), model);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<MessageDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            var deletedChatId = model.ChatId;
            var deletedMessageId = model.Id;
            await filePort.DeleteForMessageAsync(model.Id, cancellationToken);
            await unitOfWork.Messages.DeleteAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await chatNotifyPort.NotifyMessageDeletedAsync(deletedChatId, deletedMessageId, cancellationToken);
            var lastAfterDelete = (await unitOfWork.Messages.GetLastByChatIdsAsync([deletedChatId], cancellationToken))
                .GetValueOrDefault(deletedChatId);
            if (lastAfterDelete is null)
            {
                await NotifyChatListForParticipantsAsync(
                    deletedChatId,
                    senderUserId: null,
                    lastMessageText: null,
                    lastMessageAt: null,
                    lastMessageSenderName: null,
                    lastMessageHasAttachments: false,
                    lastMessageCleared: true,
                    cancellationToken);
            }
            else
            {
                var lastAttachments = await filePort.GetByMessageIdsAsync([lastAfterDelete.MessageId], cancellationToken);
                var lastAttachmentList = lastAttachments.GetValueOrDefault(lastAfterDelete.MessageId) ?? [];
                var lastSender = await userPort.GetSingleOrDefaultAsync(lastAfterDelete.SenderUserId, cancellationToken);
                await NotifyChatListForParticipantsAsync(
                    deletedChatId,
                    senderUserId: null,
                    ChatMapper.TruncatePreview(string.IsNullOrWhiteSpace(lastAfterDelete.Text)
                        ? (lastAttachmentList.Count > 0 ? "Вложение" : string.Empty)
                        : lastAfterDelete.Text),
                    lastAfterDelete.CreatedAt,
                    lastSender?.Name,
                    lastAttachmentList.Count > 0,
                    lastMessageCleared: false,
                    cancellationToken);
            }

            var response = new MessageDeleteResponse { IsDeleted = true };
            logger.LogInformation(MessageCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, MessageCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<MessageDeleteResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DeleteError);
        }
    }

    public async Task<ResponseInfo<MessageInfoPageResponse>> DisplayInfoPageAsync(MessageInfoPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.DisplayInfoLoading);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryInfoPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.BadRequest<MessageInfoPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Unauthorized<MessageInfoPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<MessageInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessParticipant(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Forbidden<MessageInfoPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = MessageMapper.ToInfoPageResponse(model);
            logger.LogInformation(MessageCrudTexts.Messages.Success.DisplayInfoCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, MessageCrudTexts.Messages.Success.DisplayInfoCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.DisplayInfo);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DisplayInfoError);
            return ResponseInfo.Error<MessageInfoPageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayInfoError);
        }
    }

    public async Task<ResponseInfo<MessageListPageResponse>> DisplayListPageAsync(MessageListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(MessageCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = MessageCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<MessageListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var currentUserErrors = MessageCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<MessageListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<MessageListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = request.ChatId,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = MessageCrudValidators.ValidateAccessParticipant(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Forbidden<MessageListPageResponse>(MessageType.FORBIDDEN, messageText);
            }

            if (request.BeforeId is Guid beforeId)
            {
                var cursor = await unitOfWork.Messages.GetSingleOrDefaultAsync(beforeId, cancellationToken);
                if (cursor is null || cursor.ChatId != request.ChatId)
                {
                    var messageText =
                        $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{MessageCrudTexts.Messages.Validation.MessageNotFoundById}";
                    logger.LogError("{Message}", messageText);
                    return ResponseInfo.NotFound<MessageListPageResponse>(MessageType.NOT_FOUND, messageText);
                }
            }

            var messageOptions = new MessageQueryOptions
            {
                ChatId = request.ChatId,
                BeforeId = request.BeforeId,
                Take = MessageListPaging.PageSize + 1,
            };
            var models = (await unitOfWork.Messages.GetListAsync(messageOptions, cancellationToken)).ToList();
            var hasOlder = models.Count > MessageListPaging.PageSize;
            if (hasOlder)
            {
                models = [.. models.Skip(1)];
            }

            var senders = await userPort.GetListAsync(models.Select(model => model.Sender.UserId).Distinct(), cancellationToken);
            var attachmentsByMessage = await filePort.GetByMessageIdsAsync(models.Select(model => model.Id), cancellationToken);
            ICollection<MessageListModel> rows =
            [
                .. models.Select(model =>
                {
                    var attachments = attachmentsByMessage.GetValueOrDefault(model.Id) ?? [];
                    return MessageMapper.ToListModel(
                        model,
                        senders.GetValueOrDefault(model.Sender.UserId),
                        [.. attachments.Select(MessageMapper.ToAttachmentModel)]);
                }),
            ];
            var response = new MessageListPageResponse
            {
                Rows = rows,
                RowExists = rows.Count > 0,
                RowCount = rows.Count,
                HasOlder = hasOlder,
            };

            if (request.BeforeId is null)
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                await unitOfWork.Recipients.MarkDeliveredAsync(
                    user.Id,
                    request.ChatId,
                    rows.Select(row => row.Id).ToList(),
                    now,
                    cancellationToken);
                var markedRead = await unitOfWork.Recipients.MarkReadForChatAsync(
                    user.Id,
                    request.ChatId,
                    now,
                    cancellationToken);
                if (markedRead > 0)
                {
                    var unread = await unitOfWork.Recipients.GetUnreadCountAsync(user.Id, request.ChatId, cancellationToken);
                    await chatNotifyPort.NotifyChatListUpdatedAsync(
                        new ChatNotifyListModel
                        {
                            ChatId = chat!.Id,
                            RoomId = chat.RoomId,
                            UnreadCount = unread,
                            TargetUserId = user.Id,
                        },
                        cancellationToken);
                }
            }

            logger.LogInformation(MessageCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, MessageCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(MessageCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessageCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<MessageListPageResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayListError);
        }
    }

    public async Task<ResponseInfo<MessageDeliveredResponse>> MarkDeliveredAsync(
        MessageDeliveredRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ChatId == Guid.Empty || request.MessageIds is null || request.MessageIds.Count == 0)
            {
                return ResponseInfo.BadRequest<MessageDeliveredResponse>(
                    MessageType.BAD_REQUEST,
                    MessageCrudTexts.Messages.Error.DisplayListError);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            if (MessageCrudValidators.ValidateCurrentUser(user).Any())
            {
                return ResponseInfo.Unauthorized<MessageDeliveredResponse>(
                    MessageType.UNAUTHORIZED,
                    MessageCrudTexts.Messages.Validation.CurrentUserNotFound);
            }

            var updated = await unitOfWork.Recipients.MarkDeliveredAsync(
                user!.Id,
                request.ChatId,
                request.MessageIds.Where(id => id != Guid.Empty).Distinct().ToList(),
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);

            return ResponseInfo.Success(
                new MessageDeliveredResponse { UpdatedCount = updated },
                MessageType.SAVED,
                MessageCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MarkDelivered failed");
            return ResponseInfo.Error<MessageDeliveredResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayListError);
        }
    }

    public async Task<ResponseInfo<MessageReadResponse>> MarkReadAsync(
        MessageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ChatId == Guid.Empty)
            {
                return ResponseInfo.BadRequest<MessageReadResponse>(
                    MessageType.BAD_REQUEST,
                    MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await userPort.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            if (MessageCrudValidators.ValidateCurrentUser(user).Any())
            {
                return ResponseInfo.Unauthorized<MessageReadResponse>(
                    MessageType.UNAUTHORIZED,
                    MessageCrudTexts.Messages.Validation.CurrentUserNotFound);
            }

            var updated = await unitOfWork.Recipients.MarkReadForChatAsync(
                user!.Id,
                request.ChatId,
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
            var unread = await unitOfWork.Recipients.GetUnreadCountAsync(user.Id, request.ChatId, cancellationToken);

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            if (chat is not null)
            {
                await chatNotifyPort.NotifyChatListUpdatedAsync(
                    new ChatNotifyListModel
                    {
                        ChatId = chat.Id,
                        RoomId = chat.RoomId,
                        UnreadCount = unread,
                        TargetUserId = user.Id,
                    },
                    cancellationToken);
            }

            return ResponseInfo.Success(
                new MessageReadResponse { UpdatedCount = updated, UnreadCount = unread },
                MessageType.SAVED,
                MessageCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MarkRead failed");
            return ResponseInfo.Error<MessageReadResponse>(MessageType.ERROR, MessageCrudTexts.Messages.Error.DisplayListError);
        }
    }

    private static ChatNotifyMessageModel ToNotifyMessage(
        Message model,
        string senderName,
        IReadOnlyList<FilePortAttachment> attachments)
    {
        return new ChatNotifyMessageModel
        {
            ChatId = model.ChatId,
            Id = model.Id,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
            SenderUserId = model.Sender.UserId,
            SenderName = senderName,
            Attachments =
            [
                .. attachments.Select(attachment => new ChatNotifyAttachmentModel
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    SizeBytes = attachment.SizeBytes,
                }),
            ],
        };
    }

    private async Task NotifyChatListForParticipantsAsync(
        Guid chatId,
        Guid? senderUserId,
        string? lastMessageText,
        DateTime? lastMessageAt,
        string? lastMessageSenderName,
        bool lastMessageHasAttachments,
        bool lastMessageCleared,
        CancellationToken cancellationToken)
    {
        var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        var participants = await unitOfWork.Participants.GetListAsync(
            new ParticipantQueryOptions { ChatId = chatId },
            cancellationToken);

        foreach (var participant in participants)
        {
            var unread = await unitOfWork.Recipients.GetUnreadCountAsync(participant.UserId, chatId, cancellationToken);
            if (senderUserId is Guid sender && participant.UserId == sender)
            {
                // отправителю превью обновляем, unread свой не трогаем смыслом — считаем из БД
            }

            await chatNotifyPort.NotifyChatListUpdatedAsync(
                new ChatNotifyListModel
                {
                    ChatId = chat.Id,
                    RoomId = chat.RoomId,
                    TargetUserId = participant.UserId,
                    UnreadCount = unread,
                    LastMessageText = lastMessageCleared ? null : lastMessageText,
                    LastMessageAt = lastMessageCleared ? null : lastMessageAt,
                    LastMessageSenderName = lastMessageCleared ? null : lastMessageSenderName,
                    LastMessageHasAttachments = !lastMessageCleared && lastMessageHasAttachments,
                    LastMessageCleared = lastMessageCleared,
                },
                cancellationToken);
        }
    }
}
