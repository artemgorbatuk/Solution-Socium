using Datasource.Security.Ef.Models;
using Microsoft.Extensions.Logging;
using Repositories.Security.Ef.Api;
using Repositories.Security.Ef.Options;
using Services.Security.Mapping;
using Services.Security.Models;
using Services.Security.Normalizers;
using Services.Security.Passwords.Providers;
using Services.Security.Texts;
using Services.Security.Tokens.Providers;
using Services.Security.Validation;
using Services.Shared.Api;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Security.Api;

/// <summary>
/// Вход, обновление токенов, выход и профиль текущего пользователя.
/// Refresh-токен ротируется при каждом обновлении; предъявление уже отозванного токена
/// считается кражей и отзывает все токены пользователя.
/// </summary>
public interface IServiceAccount
{
    Task<ResponseInfo<AccountTokenResponse>> LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AccountTokenResponse>> RefreshAsync(AccountRefreshRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AccountLogoutResponse>> LogoutAsync(AccountLogoutRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AccountProfileResponse>> DisplayProfilePageAsync(AccountProfilePageRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<AccountProfileResponse>> UpdateProfileAsync(AccountProfileUpdateRequest request, CancellationToken cancellationToken = default);
}

public class ServiceAccount : IServiceAccount
{
    private readonly IUnitOfWorkSecurity unitOfWork;
    private readonly ICurrentUser currentUser;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAccessTokenProvider accessTokenProvider;
    private readonly IRefreshTokenProvider refreshTokenProvider;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceAccount> logger;

    public ServiceAccount(
        IUnitOfWorkSecurity unitOfWork,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenProvider refreshTokenProvider,
        TimeProvider timeProvider,
        ILogger<ServiceAccount> logger)
    {
        this.unitOfWork = unitOfWork;
        this.currentUser = currentUser;
        this.passwordHasher = passwordHasher;
        this.accessTokenProvider = accessTokenProvider;
        this.refreshTokenProvider = refreshTokenProvider;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ResponseInfo<AccountTokenResponse>> LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AccountTexts.Messages.Start.LoggingIn);

        try
        {
            var primaryErrors = AccountValidators.ValidatePrimaryLoginRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{AccountTexts.Messages.Error.LoginError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.LoginError, errors);
                return ResponseInfo.BadRequest<AccountTokenResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var loginNormalized = UserNormalizer.Login(request.Login);
            var user = await unitOfWork.Users.GetSingleByLoginOrDefaultAsync(loginNormalized, cancellationToken);
            var isPasswordValid = user != null
                && !string.IsNullOrEmpty(request.Password)
                && passwordHasher.Verify(user.PasswordHash, request.Password);
            var credentialErrors = AccountValidators.ValidateCredentials(user, isPasswordValid);
            if (credentialErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, credentialErrors);
                var messageText = $"{AccountTexts.Messages.Error.LoginError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.LoginError, errors);
                return ResponseInfo.Unauthorized<AccountTokenResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var response = await IssueTokensAsync(user!, request.RememberMe, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AccountTexts.Messages.Success.LoginCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, AccountTexts.Messages.Success.LoginCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AccountTexts.Messages.Canceled.Login);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AccountTexts.Messages.Error.LoginError);
            return ResponseInfo.Error<AccountTokenResponse>(MessageType.ERROR, AccountTexts.Messages.Error.LoginError);
        }
    }

    /// <summary>
    /// Отсутствующий токен — 401, а не 400: для клиента это «не вошёл», а не ошибка запроса.
    /// </summary>
    public async Task<ResponseInfo<AccountTokenResponse>> RefreshAsync(AccountRefreshRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AccountTexts.Messages.Start.Refreshing);

        try
        {
            var token = string.IsNullOrEmpty(request?.RefreshToken)
                ? null
                : await unitOfWork.RefreshTokens.GetSingleByHashOrDefaultAsync(refreshTokenProvider.Hash(request.RefreshToken), cancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;

            if (AccountValidators.IsReused(token))
            {
                await RevokeAllAsync(token!.UserId, now, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var messageText = $"{AccountTexts.Messages.Error.RefreshError}{Environment.NewLine}{AccountTexts.Messages.Validation.RefreshTokenReused}";
                logger.LogWarning("{Message}: {Errors}", AccountTexts.Messages.Error.RefreshError, AccountTexts.Messages.Validation.RefreshTokenReused);
                return ResponseInfo.Unauthorized<AccountTokenResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var tokenErrors = AccountValidators.ValidateRefreshToken(token, now);
            if (tokenErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, tokenErrors);
                var messageText = $"{AccountTexts.Messages.Error.RefreshError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.RefreshError, errors);
                return ResponseInfo.Unauthorized<AccountTokenResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var response = await IssueTokensAsync(token!.User, token.IsPersistent, token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AccountTexts.Messages.Success.RefreshCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, AccountTexts.Messages.Success.RefreshCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AccountTexts.Messages.Canceled.Refresh);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AccountTexts.Messages.Error.RefreshError);
            return ResponseInfo.Error<AccountTokenResponse>(MessageType.ERROR, AccountTexts.Messages.Error.RefreshError);
        }
    }

    /// <summary>
    /// Идемпотентен: без токена или с уже отозванным токеном выход тоже считается выполненным.
    /// </summary>
    public async Task<ResponseInfo<AccountLogoutResponse>> LogoutAsync(AccountLogoutRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AccountTexts.Messages.Start.LoggingOut);

        try
        {
            var token = string.IsNullOrEmpty(request?.RefreshToken)
                ? null
                : await unitOfWork.RefreshTokens.GetSingleByHashOrDefaultAsync(refreshTokenProvider.Hash(request.RefreshToken), cancellationToken);

            if (token != null && token.RevokedAt == null)
            {
                RefreshTokenMapper.Revoke(token, timeProvider.GetUtcNow().UtcDateTime);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var response = new AccountLogoutResponse { IsLoggedOut = true };
            logger.LogInformation(AccountTexts.Messages.Success.LogoutCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, AccountTexts.Messages.Success.LogoutCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AccountTexts.Messages.Canceled.Logout);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AccountTexts.Messages.Error.LogoutError);
            return ResponseInfo.Error<AccountLogoutResponse>(MessageType.ERROR, AccountTexts.Messages.Error.LogoutError);
        }
    }

    public async Task<ResponseInfo<AccountProfileResponse>> DisplayProfilePageAsync(AccountProfilePageRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AccountTexts.Messages.Start.DisplayProfileLoading);

        try
        {
            var primaryErrors = AccountValidators.ValidatePrimaryProfilePageRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{AccountTexts.Messages.Error.DisplayProfileError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.DisplayProfileError, errors);
                return ResponseInfo.BadRequest<AccountProfileResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = AccountValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{AccountTexts.Messages.Error.DisplayProfileError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.DisplayProfileError, errors);
                return ResponseInfo.Unauthorized<AccountProfileResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var response = UserMapper.ToProfileResponse(user!);
            logger.LogInformation(AccountTexts.Messages.Success.DisplayProfileCompleted);
            return ResponseInfo.Success(response, MessageType.LOADED, AccountTexts.Messages.Success.DisplayProfileCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AccountTexts.Messages.Canceled.DisplayProfile);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AccountTexts.Messages.Error.DisplayProfileError);
            return ResponseInfo.Error<AccountProfileResponse>(MessageType.ERROR, AccountTexts.Messages.Error.DisplayProfileError);
        }
    }

    public async Task<ResponseInfo<AccountProfileResponse>> UpdateProfileAsync(AccountProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(AccountTexts.Messages.Start.ProfileUpdating);

        try
        {
            var primaryErrors = AccountValidators.ValidatePrimaryProfileUpdateRequest(request);
            if (primaryErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, primaryErrors);
                var messageText = $"{AccountTexts.Messages.Error.ProfileUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.ProfileUpdateError, errors);
                return ResponseInfo.BadRequest<AccountProfileResponse>(MessageType.BAD_REQUEST, messageText);
            }

            var user = await GetCurrentUserAsync(cancellationToken);
            var currentUserErrors = AccountValidators.ValidateCurrentUser(user);
            if (currentUserErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, currentUserErrors);
                var messageText = $"{AccountTexts.Messages.Error.ProfileUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.ProfileUpdateError, errors);
                return ResponseInfo.Unauthorized<AccountProfileResponse>(MessageType.UNAUTHORIZED, messageText);
            }

            var domainErrors = AccountValidators.ValidateDomainProfileUpdateRequest(request);
            if (domainErrors.Any())
            {
                var errors = string.Join(Environment.NewLine, domainErrors);
                var messageText = $"{AccountTexts.Messages.Error.ProfileUpdateError}{Environment.NewLine}{errors}";
                logger.LogError("{Message}: {Errors}", AccountTexts.Messages.Error.ProfileUpdateError, errors);
                return ResponseInfo.Invalid<AccountProfileResponse>(MessageType.INVALID, messageText);
            }

            UserMapper.Apply(user!, user!.Login, UserNormalizer.Name(request.Name));
            await unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = UserMapper.ToProfileResponse(user);
            logger.LogInformation(AccountTexts.Messages.Success.ProfileUpdateCompleted);
            return ResponseInfo.Success(response, MessageType.SAVED, AccountTexts.Messages.Success.ProfileUpdateCompleted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(AccountTexts.Messages.Canceled.ProfileUpdate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", AccountTexts.Messages.Error.ProfileUpdateError);
            return ResponseInfo.Error<AccountProfileResponse>(MessageType.ERROR, AccountTexts.Messages.Error.ProfileUpdateError);
        }
    }

    /// <summary>
    /// Выпускает пару токенов; при ротации отзывает предыдущий refresh-токен со ссылкой на новый. Сохраняет вызывающий.
    /// </summary>
    private async Task<AccountTokenResponse> IssueTokensAsync(User user, bool isPersistent, RefreshToken? previous, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var accessToken = accessTokenProvider.Create(user.Id);
        var refreshToken = refreshTokenProvider.Create();

        var tokenOptions = new RefreshTokenGetNewOptions
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            IsPersistent = isPersistent,
            CreatedAt = now,
            ExpiresAt = refreshToken.ExpiresAt,
        };
        var model = unitOfWork.RefreshTokens.GetNew(tokenOptions);
        await unitOfWork.RefreshTokens.CreateAsync(model, cancellationToken);

        if (previous != null)
        {
            RefreshTokenMapper.Revoke(previous, now, model.Id);
        }

        return new AccountTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            IsPersistent = isPersistent,
            User = UserMapper.ToProfileResponse(user),
        };
    }

    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var tokenOptions = new RefreshTokenQueryOptions
        {
            UserId = userId,
            IsRevoked = false,
        };
        var activeTokens = await unitOfWork.RefreshTokens.GetListAsync(tokenOptions, cancellationToken);
        foreach (var token in activeTokens)
        {
            RefreshTokenMapper.Revoke(token, now);
        }
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return currentUser.UserId is Guid currentUserId
            ? await unitOfWork.Users.GetSingleOrDefaultAsync(currentUserId, cancellationToken)
            : null;
    }
}
