using Datasource.Socium.Ef.Models;
using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Enums;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class RoleAssignmentCrudValidators
{
    public static IEnumerable<string> ValidatePrimaryListPageRequest(RoleAssignmentListPageRequest? request)
    {
        if (request == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.UserId == Guid.Empty)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateRequest(RoleAssignmentCreateRequest? request)
    {
        if (request == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        foreach (var error in ValidateTarget(request.UserId, request.RoleName, request.RoomId, request.ChatId))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(RoleAssignmentDeleteRequest? request)
    {
        if (request == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        foreach (var error in ValidateTarget(request.UserId, request.RoleName, request.RoomId, request.ChatId))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryLastAdminListRequest(RoleAssignmentLastAdminListRequest? request)
    {
        if (request == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.UserId == Guid.Empty)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteByUserRequest(RoleAssignmentDeleteByUserRequest? request)
    {
        if (request == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.UserId == Guid.Empty)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(UserPortModel? user)
    {
        if (user == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityUser(UserPortModel? user)
    {
        if (user == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityRoom(Room? room)
    {
        if (room == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RoomNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityChat(Chat? chat)
    {
        if (chat == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.ChatNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessRead(bool allowed)
    {
        if (!allowed)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.AccessDenied;
        }
    }

    /// <summary>Свои доступы админ не меняет; доступы удалённого пользователя — тоже.</summary>
    public static IEnumerable<string> ValidateAccessManage(bool allowed, UserPortModel target, Guid currentUserId)
    {
        if (!allowed)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.AccessDenied;
        }
        else if (target.Id == currentUserId)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.CannotManageSelf;
        }
        else if (target.IsDeleted)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserDeleted;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRoomAdmin(bool isRoomAdmin)
    {
        if (isRoomAdmin)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.AlreadyRoomAdmin;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateChatAdmin(Participant? participant, bool isChatAdmin)
    {
        if (participant == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.NotParticipant;
        }
        else if (isChatAdmin)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.AlreadyChatAdmin;
        }
    }

    public static IEnumerable<string> ValidateDomainDeleteRoomAdmin(bool isRoomAdmin, int roomAdminCount)
    {
        if (!isRoomAdmin)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.NotRoomAdmin;
            yield break;
        }

        foreach (var error in ValidateDomainLastRoomAdmin(isRoomAdmin, roomAdminCount))
        {
            yield return error;
        }
    }

    /// <summary>У комнаты всегда остаётся хотя бы один админ.</summary>
    public static IEnumerable<string> ValidateDomainLastRoomAdmin(bool isRoomAdmin, int roomAdminCount)
    {
        if (isRoomAdmin && roomAdminCount <= 1)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.LastRoomAdminCannotRevoke;
        }
    }

    public static IEnumerable<string> ValidateDomainDeleteParticipant(Participant? participant, bool isChatAdmin, int chatAdminCount)
    {
        if (participant == null)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.NotParticipant;
            yield break;
        }

        foreach (var error in ParticipantCrudValidators.ValidateDomainLastChatAdmin(isChatAdmin, chatAdminCount))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidateDomainDeleteChatAdmin(bool isChatAdmin, int chatAdminCount)
    {
        if (!isChatAdmin)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.NotChatAdmin;
            yield break;
        }

        foreach (var error in ParticipantCrudValidators.ValidateDomainLastChatAdmin(isChatAdmin, chatAdminCount))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidateDomainDeleteByUser(UserPortModel? user)
    {
        if (user is { IsDeleted: false })
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserNotDeleted;
        }
    }

    private static IEnumerable<string> ValidateTarget(Guid userId, string? roleName, Guid? roomId, Guid? chatId)
    {
        if (userId == Guid.Empty)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }

        var scope = !string.IsNullOrWhiteSpace(roleName) && RoleCatalog.TryGet(roleName, out var definition)
            ? definition!.Scope
            : RoleScope.App;
        var hasRoomId = roomId is Guid room && room != Guid.Empty;
        var hasChatId = chatId is Guid chat && chat != Guid.Empty;
        if (scope == RoleScope.App)
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.UnknownRole;
        }
        else if (scope == RoleScope.Room && (!hasRoomId || hasChatId))
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.RoomIdRequired;
        }
        else if (scope == RoleScope.Chat && (!hasChatId || hasRoomId))
        {
            yield return RoleAssignmentCrudTexts.Messages.Validation.ChatIdRequired;
        }
    }
}
