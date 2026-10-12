using Datasource.Security.Ef.Models;
using Services.Security.Models;
using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Enums;
using Services.Security.Texts;

namespace Services.Security.Validation;

public static class AppRoleCrudValidators
{
    public static IEnumerable<string> ValidatePrimaryCreateRequest(AppRoleCreateRequest? request)
    {
        if (request == null)
        {
            yield return AppRoleCrudTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        foreach (var error in ValidateUserAndRole(request.UserId, request.RoleName))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(AppRoleDeleteRequest? request)
    {
        if (request == null)
        {
            yield return AppRoleCrudTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        foreach (var error in ValidateUserAndRole(request.UserId, request.RoleName))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(User? user)
    {
        if (user == null || user.IsDeleted)
        {
            yield return AppRoleCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityUser(User? model)
    {
        if (model == null || model.IsDeleted)
        {
            yield return AppRoleCrudTexts.Messages.Validation.UserNotFoundById;
        }
    }

    /// <summary>Свои доступы админ не расширяет: роль выдаёт другой админ приложения.</summary>
    public static IEnumerable<string> ValidateAccessCreate(bool allowed, Guid targetUserId, Guid currentUserId)
    {
        if (!allowed)
        {
            yield return AppRoleCrudTexts.Messages.Validation.AccessDenied;
        }
        else if (targetUserId == currentUserId)
        {
            yield return AppRoleCrudTexts.Messages.Validation.CannotGrantSelf;
        }
    }

    /// <summary>С себя снять можно — если останется другой админ (<see cref="ValidateDomainLastAppAdmin"/>).</summary>
    public static IEnumerable<string> ValidateAccessDelete(bool allowed)
    {
        if (!allowed)
        {
            yield return AppRoleCrudTexts.Messages.Validation.AccessDenied;
        }
    }

    public static IEnumerable<string> ValidateDomainCreate(bool isAssigned)
    {
        if (isAssigned)
        {
            yield return AppRoleCrudTexts.Messages.Validation.AlreadyAssigned;
        }
    }

    public static IEnumerable<string> ValidateDomainDelete(bool isAssigned, int appAdminCount)
    {
        if (!isAssigned)
        {
            yield return AppRoleCrudTexts.Messages.Validation.NotAssigned;
            yield break;
        }

        foreach (var error in ValidateDomainLastAppAdmin(isAssigned, appAdminCount))
        {
            yield return error;
        }
    }

    /// <summary>Хотя бы один админ приложения остаётся всегда.</summary>
    public static IEnumerable<string> ValidateDomainLastAppAdmin(bool isAppAdmin, int appAdminCount)
    {
        if (isAppAdmin && appAdminCount <= 1)
        {
            yield return AppRoleCrudTexts.Messages.Validation.LastAppAdminCannotRevoke;
        }
    }

    private static IEnumerable<string> ValidateUserAndRole(Guid userId, string? roleName)
    {
        if (userId == Guid.Empty)
        {
            yield return AppRoleCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }

        var isAppRole = !string.IsNullOrWhiteSpace(roleName)
            && RoleCatalog.TryGet(roleName, out var definition)
            && definition!.Scope == RoleScope.App;
        if (!isAppRole)
        {
            yield return AppRoleCrudTexts.Messages.Validation.UnknownRole;
        }
    }
}
