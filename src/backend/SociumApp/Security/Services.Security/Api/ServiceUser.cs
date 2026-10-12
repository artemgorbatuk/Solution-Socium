using Datasource.Security.Ef.Models;
using Microsoft.Extensions.Logging;
using Repositories.Security.Ef.Api;
using Repositories.Security.Ef.Options;
using Services.Security.Mapping;
using Services.Security.Models;
using Services.Security.Normalizers;
using Services.Security.Passwords.Providers;
using Services.Security.Texts;
using Services.Security.Ports;
using Services.Security.Validation;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Security.Api;

/// <summary>
/// Создание — регистрация, доступна без входа; остальное — только вошедшему.
/// Изменить и удалить пользователя может только он сам.
/// </summary>
public interface IServiceUser
{
    Task<ResponseInfo<UserCreatePageResponse>> DisplayCreatePageAsync(UserCreatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserCreatePageResponse>> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserUpdatePageResponse>> DisplayUpdatePageAsync(UserUpdatePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserUpdatePageResponse>> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserDeletePageResponse>> DisplayDeletePageAsync(UserDeletePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserDeleteResponse>> DeleteAsync(UserDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserInfoPageResponse>> DisplayInfoPageAsync(UserInfoPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserListPageResponse>> DisplayListPageAsync(UserListPageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<UserSearchResponse>> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken = default);
}

public class ServiceUser : IServiceUser
{
    private readonly IUnitOfWorkSecurity unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IPasswordHasher passwordHasher;
    private readonly IEmailPort emailPort;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceUser> logger;

    public ServiceUser(
        IUnitOfWorkSecurity unitOfWork,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IEmailPort emailPort,
        TimeProvider timeProvider,
        ILogger<ServiceUser> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.passwordHasher = passwordHasher;
        this.emailPort = emailPort;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<ResponseInfo<UserCreatePageResponse>> DisplayCreatePageAsync(UserCreatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.DisplayCreating);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryCreatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayCreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayCreateError, errors);
                return Task.FromResult(ResponseInfo.BadRequest<UserCreatePageResponse>(MessageType.BAD_REQUEST, messageText));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var model = unitOfWork.Users.GetNew();
            var response = UserMapper.ToCreatePageResponse(model);
            logger.LogInformation(UserCrudTexts.Messages.Success.DisplayCreateCompleted);
            return Task.FromResult(ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.DisplayCreateCompleted));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.DisplayCreate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DisplayCreateError);
            return Task.FromResult(ResponseInfo.Error<UserCreatePageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DisplayCreateError));
        }
    }

    public async Task<ResponseInfo<UserCreatePageResponse>> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.Creating);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryCreateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.BadRequest<UserCreatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var domainErrors = UserCrudValidators.ValidateDomainCreateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<UserCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var loginNormalized = UserNormalizer.Login(request.Login);
            var emailNormalized = UserNormalizer.Email(request.Email);
            var nameNormalized = UserNormalizer.Name(request.Name);
            var loginDuplicates = await unitOfWork.Users.GetListAsync(new UserQueryOptions { Login = loginNormalized }, cancellationToken);
            var emailDuplicates = await unitOfWork.Users.GetListAsync(new UserQueryOptions { Email = emailNormalized }, cancellationToken);
            var duplicateErrors = UserCrudValidators.ValidateDuplicates(loginDuplicates.Any(), emailDuplicates.Any());
            if (duplicateErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, duplicateErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.CreateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.CreateError, errors);
                return ResponseInfo.Invalid<UserCreatePageResponse>(MessageType.INVALID, messageText);
            }

            var model = unitOfWork.Users.GetNew();
            UserMapper.Apply(model, loginNormalized, emailNormalized, nameNormalized);
            model.PasswordHash = passwordHasher.Hash(request.Password);
            model.IsEmailConfirmed = false;
            await unitOfWork.Users.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Письмо не блокирует регистрацию: при сбое SMTP пользователь сможет нажать «Выслать ещё раз».
            var emailResult = await emailPort.SendConfirmationAsync(model.Id, model.Email, model.Name, cancellationToken);
            if (emailResult.MessageInfo.MessageType == MessageType.ERROR)
            {
                logger.LogWarning("{Message}", emailResult.MessageInfo.MessageText);
            }

            logger.LogInformation(UserCrudTexts.Messages.Success.CreateCompleted);
            return ResponseInfo.Success(UserMapper.ToCreatePageResponse(model), MessageType.SAVED, UserCrudTexts.Messages.Success.CreateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.Create);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.CreateError);
            return ResponseInfo.Error<UserCreatePageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.CreateError);
        }
    }

    public async Task<ResponseInfo<UserUpdatePageResponse>> DisplayUpdatePageAsync(UserUpdatePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.DisplayUpdating);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryUpdatePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.BadRequest<UserUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Unauthorized<UserUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Users.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = UserCrudValidators.ValidateAccessibilityUser(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.NotFound<UserUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var accessErrors = UserCrudValidators.ValidateAccessSelf(model!, user!);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayUpdateError, errors);
                return ResponseInfo.Forbidden<UserUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = UserMapper.ToUpdatePageResponse(model!);
            logger.LogInformation(UserCrudTexts.Messages.Success.DisplayUpdateCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.DisplayUpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.DisplayUpdate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DisplayUpdateError);
            return ResponseInfo.Error<UserUpdatePageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DisplayUpdateError);
        }
    }

    public async Task<ResponseInfo<UserUpdatePageResponse>> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.Updating);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.BadRequest<UserUpdatePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Unauthorized<UserUpdatePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Users.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = UserCrudValidators.ValidateAccessibilityUser(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.NotFound<UserUpdatePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var accessErrors = UserCrudValidators.ValidateAccessSelf(model!, user!);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Forbidden<UserUpdatePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var domainErrors = UserCrudValidators.ValidateDomainUpdateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<UserUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            var loginNormalized = UserNormalizer.Login(request.Login);
            var nameNormalized = UserNormalizer.Name(request.Name);
            var duplicateOptions = new UserQueryOptions
            {
                Login = loginNormalized,
                ExcludeId = request.Id,
            };
            var duplicates = await unitOfWork.Users.GetListAsync(duplicateOptions, cancellationToken);
            var duplicateErrors = UserCrudValidators.ValidateDuplicates(duplicates.Any());
            if (duplicateErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, duplicateErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.UpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.UpdateError, errors);
                return ResponseInfo.Invalid<UserUpdatePageResponse>(MessageType.INVALID, messageText);
            }

            UserMapper.Apply(model!, loginNormalized, model!.Email, nameNormalized);
            await unitOfWork.Users.UpdateAsync(model!, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(UserCrudTexts.Messages.Success.UpdateCompleted);
            return ResponseInfo.Success<UserUpdatePageResponse>(MessageType.SAVED, UserCrudTexts.Messages.Success.UpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.Update);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.UpdateError);
            return ResponseInfo.Error<UserUpdatePageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.UpdateError);
        }
    }

    public async Task<ResponseInfo<UserDeletePageResponse>> DisplayDeletePageAsync(UserDeletePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.DisplayDeleting);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryDeletePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.BadRequest<UserDeletePageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Unauthorized<UserDeletePageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Users.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = UserCrudValidators.ValidateAccessibilityUser(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.NotFound<UserDeletePageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var accessErrors = UserCrudValidators.ValidateAccessSelf(model!, user!);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayDeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayDeleteError, errors);
                return ResponseInfo.Forbidden<UserDeletePageResponse>(MessageType.FORBIDDEN, messageText);
            }

            var response = UserMapper.ToDeletePageResponse(model!);
            logger.LogInformation(UserCrudTexts.Messages.Success.DisplayDeleteCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.DisplayDeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.DisplayDelete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DisplayDeleteError);
            return ResponseInfo.Error<UserDeletePageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DisplayDeleteError);
        }
    }

    /// <summary>
    /// Мягкое удаление; все refresh-токены пользователя отзываются в том же сохранении — выход на всех устройствах.
    /// </summary>
    public async Task<ResponseInfo<UserDeleteResponse>> DeleteAsync(UserDeleteRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.Deleting);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryDeleteRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.BadRequest<UserDeleteResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Unauthorized<UserDeleteResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Users.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = UserCrudValidators.ValidateAccessibilityUser(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.NotFound<UserDeleteResponse>(MessageType.NOT_FOUND, messageText);
            }

            var accessErrors = UserCrudValidators.ValidateAccessSelf(model!, user!);
            if (accessErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DeleteError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DeleteError, errors);
                return ResponseInfo.Forbidden<UserDeleteResponse>(MessageType.FORBIDDEN, messageText);
            }

            model!.IsDeleted = true;
            await unitOfWork.Users.UpdateAsync(model, cancellationToken);
            var tokenOptions = new RefreshTokenQueryOptions
            {
                UserId = model.Id,
                IsRevoked = false,
            };
            var activeTokens = await unitOfWork.RefreshTokens.GetListAsync(tokenOptions, cancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var token in activeTokens)
            {
                RefreshTokenMapper.Revoke(token, now);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new UserDeleteResponse { IsDeleted = true };
            logger.LogInformation(UserCrudTexts.Messages.Success.DeleteCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, UserCrudTexts.Messages.Success.DeleteCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.Delete);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DeleteError);
            return ResponseInfo.Error<UserDeleteResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DeleteError);
        }
    }

    public async Task<ResponseInfo<UserInfoPageResponse>> DisplayInfoPageAsync(UserInfoPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.DisplayInfoLoading);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryInfoPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.BadRequest<UserInfoPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.Unauthorized<UserInfoPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var model = await unitOfWork.Users.GetSingleOrDefaultAsync(request.Id, cancellationToken);
            var accessibilityErrors = UserCrudValidators.ValidateAccessibilityUser(model);
            if (accessibilityErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, accessibilityErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayInfoError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayInfoError, errors);
                return ResponseInfo.NotFound<UserInfoPageResponse>(MessageType.NOT_FOUND, messageText);
            }

            var response = UserMapper.ToInfoPageResponse(model!);
            logger.LogInformation(UserCrudTexts.Messages.Success.DisplayInfoCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.DisplayInfoCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.DisplayInfo);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DisplayInfoError);
            return ResponseInfo.Error<UserInfoPageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DisplayInfoError);
        }
    }

    public async Task<ResponseInfo<UserListPageResponse>> DisplayListPageAsync(UserListPageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.DisplayListLoading);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimaryListPageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.BadRequest<UserListPageResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.DisplayListError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.DisplayListError, errors);
                return ResponseInfo.Unauthorized<UserListPageResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var userOptions = new UserQueryOptions
            {
                Ids = request.Ids,
                IsDeleted = false,
            };
            var models = await unitOfWork.Users.GetListAsync(userOptions, cancellationToken);
            ICollection<UserListModel> rows = [.. models.Select(UserMapper.ToListModel)];
            var response = new UserListPageResponse
            {
                Rows = rows,
                RowExists = rows.Count > 0,
                RowCount = rows.Count
            };

            logger.LogInformation(UserCrudTexts.Messages.Success.DisplayListCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.DisplayListCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.DisplayList);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.DisplayListError);
            return ResponseInfo.Error<UserListPageResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.DisplayListError);
        }
    }

    public async Task<ResponseInfo<UserSearchResponse>> SearchAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(UserCrudTexts.Messages.Start.Searching);

        try
        {
            var primaryErrors = UserCrudValidators.ValidatePrimarySearchRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.SearchError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.SearchError, errors);
                return ResponseInfo.BadRequest<UserSearchResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = UserCrudValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{UserCrudTexts.Messages.Error.SearchError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", UserCrudTexts.Messages.Error.SearchError, errors);
                return ResponseInfo.Unauthorized<UserSearchResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var userOptions = new UserQueryOptions
            {
                LoginContains = request.Login.Trim(),
                IsDeleted = false,
            };
            var models = await unitOfWork.Users.GetListAsync(userOptions, cancellationToken);
            ICollection<UserSearchModel> rows =
            [
                .. models.Take(20).Select(UserMapper.ToSearchModel),
            ];
            var response = UserMapper.ToSearchResponse(rows);

            logger.LogInformation(UserCrudTexts.Messages.Success.SearchCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, UserCrudTexts.Messages.Success.SearchCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(UserCrudTexts.Messages.Canceled.Search);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", UserCrudTexts.Messages.Error.SearchError);
            return ResponseInfo.Error<UserSearchResponse>(MessageType.ERROR, UserCrudTexts.Messages.Error.SearchError);
        }
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return currentUser.UserId is Guid currentUserId
            ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
            : null;
    }
}
