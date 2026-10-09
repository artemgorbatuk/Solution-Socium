using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class ChatCrudValidators
{
    private const int NameMaximumLength = 128;

    public static IEnumerable<string> ValidatePrimaryCreatePageRequest(ChatCreatePageRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.RoomId == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateRequest(ChatCreateRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.RoomId == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRequest(ChatCreateRequest request)
    {
        foreach (var error in ValidateName(request.Name))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdatePageRequest(ChatUpdatePageRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdateRequest(ChatUpdateRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainUpdateRequest(ChatUpdateRequest request)
    {
        foreach (var error in ValidateName(request.Name))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeletePageRequest(ChatDeletePageRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(ChatDeleteRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryInfoPageRequest(ChatInfoPageRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryListPageRequest(ChatListPageRequest? request)
    {
        if (request == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.RoomId == Guid.Empty)
        {
            yield return ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityChat<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return ChatCrudTexts.Messages.Validation.ChatNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityRoom<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return ChatCrudTexts.Messages.Validation.RoomNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(User? user)
    {
        if (user == null || user.IsDeleted)
        {
            yield return ChatCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    public static IEnumerable<string> ValidateAccessAdmin(Participant? participant)
    {
        if (participant == null || !participant.IsAdmin)
        {
            yield return ChatCrudTexts.Messages.Validation.NotAdmin;
        }
    }

    private static IEnumerable<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return ChatCrudTexts.Messages.Validation.NameNotEmpty;
            yield break;
        }

        if (name.Trim().Length > NameMaximumLength)
        {
            yield return ChatCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength);
        }
    }
}
