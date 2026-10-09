using Microsoft.Extensions.Logging;
using Repositories.Socium.Ef.Api;
using Repositories.Socium.Ef.Options;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Mapping;
using Services.Socium.Models;
using Services.Socium.Normalizers;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Services.Socium.Api;

public interface IServiceChat
{
    Task<ResponseInfo<ChatCreatePageResponse>> DisplayCreatePageAsync(ChatCreatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<ChatCreatePageResponse>> CreateAsync(ChatCreateRequest request, CancellationToken cancellationToken = default);
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
    private readonly ILogger<ServiceChat> logger;

    public ServiceChat(IUnitOfWorkSocium unitOfWork, ICurrentUser currentUser, ILogger<ServiceChat> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
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

            var room = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.RoomId, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityRoom(room);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.NotFound<ChatCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var newOptions = new ChatGetNewOptions
            {
                RoomId = request.RoomId,
            };
            var model = unitOfWork.Chats.GetNew(newOptions);
            var response = ChatMapper.ToCreatePageResponse(model);
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
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
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

            var domainErrors = ChatCrudValidators.ValidateDomainCreateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<ChatCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var nameNormalized = ChatNormalizer.Name(request.Name);
            var newOptions = new ChatGetNewOptions
            {
                RoomId = request.RoomId,
            };
            var model = unitOfWork.Chats.GetNew(newOptions);
            ChatMapper.Apply(model, nameNormalized);
            await unitOfWork.Chats.CreateAsync(model, cancellationToken);

            var participantNewOptions = new ParticipantGetNewOptions
            {
                ChatId = model.Id,
                UserId = user!.Id,
                IsAdmin = true,
            };
            var participant = unitOfWork.Participants.GetNew(participantNewOptions);
            await unitOfWork.Participants.CreateAsync(participant, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.Id,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessAdmin(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Forbidden<ChatUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = ChatMapper.ToUpdatePageResponse(model);
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
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.Id,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessAdmin(participants.SingleOrDefault());
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
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.Id,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessAdmin(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<ChatDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = ChatMapper.ToDeletePageResponse(model);
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
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
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

            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.Id,
                UserId = user!.Id,
            };
            var participants = await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var accessErrors = ChatCrudValidators.ValidateAccessAdmin(participants.SingleOrDefault());
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<ChatDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            await unitOfWork.Chats.DeleteAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ChatDeleteResponse { IsDeleted = true };
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

            var model = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityChat(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<ChatInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var participantOptions = new ParticipantQueryOptions
            {
                ChatId = model!.Id,
                UserId = user?.Id ?? Guid.Empty,
            };
            var participants = ChatCrudValidators.ValidateCurrentUser(user).Any()
                ? []
                : await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);

            var response = ChatMapper.ToInfoPageResponse(model, participants.SingleOrDefault());
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

            var room = await unitOfWork.Rooms.GetSingleOrDefaultAsync(request.RoomId, cancellationToken);
            var accessibilityErrors = ChatCrudValidators.ValidateAccessibilityRoom(room);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{ChatCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", ChatCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<ChatListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var user = currentUser.UserId is Guid currentUserId
                ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
                : null;
            var participantOptions = new ParticipantQueryOptions
            {
                UserId = user?.Id ?? Guid.Empty,
                IsAdmin = true,
            };
            var adminParticipants = ChatCrudValidators.ValidateCurrentUser(user).Any()
                ? []
                : await unitOfWork.Participants.GetListAsync(participantOptions, cancellationToken);
            var adminChatIds = adminParticipants.Select(participant => participant.ChatId).ToHashSet();

            var chatOptions = new ChatQueryOptions
            {
                RoomId = request.RoomId,
            };
            var models = await unitOfWork.Chats.GetListAsync(chatOptions, cancellationToken);
            ICollection<ChatListModel> rows = [.. models.Select(model => ChatMapper.ToListModel(model, adminChatIds.Contains(model.Id)))];
            var response = new ChatListPageResponse
            {
                Rows = rows,
                RowExists = rows.Count > 0,
                RowCount = rows.Count
            };

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
