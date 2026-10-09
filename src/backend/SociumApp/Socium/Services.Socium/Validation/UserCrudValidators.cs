using System.Text.RegularExpressions;
using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static partial class UserCrudValidators
{
    private const int LoginMinimumLength = 3;
    private const int LoginMaximumLength = 64;
    private const int NameMaximumLength = 128;

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
        foreach (var error in ValidateLogin(request.Login).Concat(ValidateName(request.Name)))
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

    /// <summary>Удалённый пользователь для CRUD не существует — как и отсутствующий.</summary>
    public static IEnumerable<string> ValidateAccessibilityUser(User? model)
    {
        if (model == null || model.IsDeleted)
        {
            yield return UserCrudTexts.Messages.Validation.UserNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateDuplicates(bool hasDuplicate)
    {
        if (hasDuplicate)
        {
            yield return UserCrudTexts.Messages.Validation.LoginAlreadyExists;
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

    private static IEnumerable<string> ValidateName(string? name)
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

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex LoginPattern();
}
