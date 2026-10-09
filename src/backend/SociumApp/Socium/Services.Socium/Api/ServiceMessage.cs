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
}

public class ServiceMessage : IServiceMessage
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceMessage> logger;

    public ServiceMessage(IUnitOfWorkSocium unitOfWork, TimeProvider timeProvider, ILogger<ServiceMessage> logger)
    {
        this.unitOfWork = unitOfWork;
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

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayCreateError, errors);
                return ResponseInfo.NotFound<MessageCreatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var newOptions = new MessageGetNewOptions
            {
                ChatId = request.ChatId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
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

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.NotFound<MessageCreatePageResponse>(MessageType.NOT_FOUND, messageText);
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
            var newOptions = new MessageGetNewOptions
            {
                ChatId = request.ChatId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };
            var model = unitOfWork.Messages.GetNew(newOptions);
            MessageMapper.Apply(model, textNormalized);
            await unitOfWork.Messages.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.NotFound<MessageUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var response = MessageMapper.ToUpdatePageResponse(model!);
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

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<MessageUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
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
            MessageMapper.Apply(model!, textNormalized);
            await unitOfWork.Messages.UpdateAsync(model!, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<MessageDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var response = MessageMapper.ToDeletePageResponse(model!);
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

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<MessageDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            await unitOfWork.Messages.DeleteAsync(model!, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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

            var model = await unitOfWork.Messages.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityMessage(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<MessageInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var response = MessageMapper.ToInfoPageResponse(model!);
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

            var chat = await unitOfWork.Chats.GetSingleOrDefaultAsync(request.ChatId, cancellationToken);
            var accessibilityErrors = MessageCrudValidators.ValidateAccessibilityChat(chat);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{MessageCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", MessageCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.NotFound<MessageListPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var messageOptions = new MessageQueryOptions
            {
                ChatId = request.ChatId,
            };
            var models = await unitOfWork.Messages.GetListAsync(messageOptions, cancellationToken);
            ICollection<MessageListModel> rows = [.. models.Select(MessageMapper.ToListModel)];
            var response = new MessageListPageResponse
            {
                Rows = rows,
                RowExists = rows.Count > 0,
                RowCount = rows.Count
            };

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
}
