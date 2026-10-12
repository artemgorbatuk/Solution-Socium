using System.Text.RegularExpressions;
using Datasource.Security.Ef.Models;
using Services.Security.Models;
using Services.Security.Texts;

namespace Services.Security.Validation;

public static partial class UserCrudValidators
{
    private const int LoginMinimumLength = 3;
    private const int LoginMaximumLength = 64;
    private const int EmailMaximumLength = 256;
    private const int NameMaximumLength = 128;
    private const int PasswordMinimumLength = 8;
    private const int PasswordMaximumLength = 128;

    public static IEnumerable<string> ValidatePrimaryCreatePageRequest(UserCreatePageRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateRequest(UserCreateRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRequest(UserCreateRequest request)
    {
        foreach (var error in ValidateEmail(request.Email)
            .Concat(ValidateLogin(request.Login))
            .Concat(ValidateName(request.Name))
            .Concat(ValidatePassword(request.Password)))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdatePageRequest(UserUpdatePageRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return UserCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdateRequest(UserUpdateRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return UserCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainUpdateRequest(UserUpdateRequest request)
    {
        foreach (var error in ValidateLogin(request.Login).Concat(ValidateName(request.Name)))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeletePageRequest(UserDeletePageRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return UserCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(UserDeleteRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return UserCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryInfoPageRequest(UserInfoPageRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return UserCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryListPageRequest(UserListPageRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimarySearchRequest(UserSearchRequest? request)
    {
        if (request == null)
        {
            yield return UserCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (string.IsNullOrWhiteSpace(request.Login))
        {
            yield return UserCrudTexts.Messages.Validation.SearchLoginNotEmpty;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(User? user)
    {
        if (user == null || user.IsDeleted)
        {
            yield return UserCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    /// <summary>Удалённый пользователь для CRUD не существует — как и отсутствующий.</summary>
    public static IEnumerable<string> ValidateAccessibilityUser(User? model)
    {
        if (model == null || model.IsDeleted)
        {
            yield return UserCrudTexts.Messages.Validation.UserNotFoundById;
        }
    }

    /// <summary>Изменить и удалить пользователя может только он сам.</summary>
    public static IEnumerable<string> ValidateAccessSelf(User model, User currentUser)
    {
        if (model.Id != currentUser.Id)
        {
            yield return UserCrudTexts.Messages.Validation.NotSelf;
        }
    }

    public static IEnumerable<string> ValidateDuplicates(bool hasDuplicateLogin, bool hasDuplicateEmail = false)
    {
        if (hasDuplicateLogin)
        {
            yield return UserCrudTexts.Messages.Validation.LoginAlreadyExists;
        }

        if (hasDuplicateEmail)
        {
            yield return UserCrudTexts.Messages.Validation.EmailAlreadyExists;
        }
    }

    public static IEnumerable<string> ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            yield return UserCrudTexts.Messages.Validation.EmailNotEmpty;
            yield break;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaximumLength)
        {
            yield return UserCrudTexts.Messages.Validation.EmailMaximumLength(EmailMaximumLength);
        }

        if (!EmailPattern().IsMatch(trimmed))
        {
            yield return UserCrudTexts.Messages.Validation.EmailFormat;
        }
    }

    public static IEnumerable<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return UserCrudTexts.Messages.Validation.NameNotEmpty;
            yield break;
        }

        if (name.Trim().Length > NameMaximumLength)
        {
            yield return UserCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength);
        }
    }

    private static IEnumerable<string> ValidateLogin(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            yield return UserCrudTexts.Messages.Validation.LoginNotEmpty;
            yield break;
        }

        var trimmed = login.Trim();
        if (trimmed.Length is < LoginMinimumLength or > LoginMaximumLength)
        {
            yield return UserCrudTexts.Messages.Validation.LoginLength(LoginMinimumLength, LoginMaximumLength);
        }

        if (!LoginPattern().IsMatch(trimmed))
        {
            yield return UserCrudTexts.Messages.Validation.LoginFormat;
        }
    }

    /// <summary>Пароль не обрезается: пробелы по краям — часть пароля.</summary>
    public static IEnumerable<string> ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            yield return UserCrudTexts.Messages.Validation.PasswordNotEmpty;
            yield break;
        }

        if (password.Length is < PasswordMinimumLength or > PasswordMaximumLength)
        {
            yield return UserCrudTexts.Messages.Validation.PasswordLength(PasswordMinimumLength, PasswordMaximumLength);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex LoginPattern();

    // Простая проверка формы: локальная часть, @, домен с точкой.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
