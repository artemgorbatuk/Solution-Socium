using Datasource.Security.Ef.Models;
using Services.Security.Models;
using Services.Security.Texts;

namespace Services.Security.Validation;

public static class AccountValidators
{
    public static IEnumerable<string> ValidatePrimaryLoginRequest(AccountLoginRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    /// <summary>
    /// Одна ошибка на неизвестный логин, удалённого, неподтверждённый email и неверный пароль —
    /// чтобы по ответу нельзя было подобрать логины или узнать статус почты.
    /// </summary>
    public static IEnumerable<string> ValidateCredentials(User? user, bool isPasswordValid)
    {
        if (user == null || user.IsDeleted || !user.IsEmailConfirmed || !isPasswordValid)
        {
            yield return AccountTexts.Messages.Validation.InvalidCredentials;
        }
    }

    public static IEnumerable<string> ValidatePrimaryConfirmEmailRequest(AccountConfirmEmailRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (string.IsNullOrWhiteSpace(request.Token))
        {
            yield return AccountTexts.Messages.Validation.TokenRequired;
        }
    }

    public static IEnumerable<string> ValidatePrimaryResendConfirmationRequest(AccountResendConfirmationRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimaryForgotPasswordRequest(AccountForgotPasswordRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimaryResetPasswordRequest(AccountResetPasswordRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (string.IsNullOrWhiteSpace(request.Token))
        {
            yield return AccountTexts.Messages.Validation.TokenRequired;
        }
    }

    public static IEnumerable<string> ValidateDomainResetPasswordRequest(AccountResetPasswordRequest request)
    {
        return UserCrudValidators.ValidatePassword(request.Password);
    }

    public static IEnumerable<string> ValidatePrimaryProfilePageRequest(AccountProfilePageRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimaryProfileUpdateRequest(AccountProfileUpdateRequest? request)
    {
        if (request == null)
        {
            yield return AccountTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidateDomainProfileUpdateRequest(AccountProfileUpdateRequest request)
    {
        return UserCrudValidators.ValidateName(request.Name);
    }

    public static IEnumerable<string> ValidateCurrentUser(User? user)
    {
        if (user == null || user.IsDeleted)
        {
            yield return AccountTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    /// <summary>
    /// Отсутствующий, истёкший токен и токен удалённого пользователя — одинаково «сессия истекла».
    /// Отозванный токен проверяется отдельно: <see cref="IsReused"/>.
    /// </summary>
    public static IEnumerable<string> ValidateRefreshToken(RefreshToken? token, DateTime now)
    {
        if (token == null || token.ExpiresAt <= now || token.User.IsDeleted)
        {
            yield return AccountTexts.Messages.Validation.RefreshTokenInvalid;
        }
    }

    public static bool IsReused(RefreshToken? token)
    {
        return token?.RevokedAt != null;
    }
}
