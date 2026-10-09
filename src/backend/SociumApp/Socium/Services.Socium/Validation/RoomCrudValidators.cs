using Services.Socium.Models;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class RoomCrudValidators
{
    private const int NameMaximumLength = 128;

    public static IEnumerable<string> ValidatePrimaryCreatePageRequest(RoomCreatePageRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateRequest(RoomCreateRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRequest(RoomCreateRequest request)
    {
        foreach (var error in ValidateName(request.Name))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdatePageRequest(RoomUpdatePageRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return RoomCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdateRequest(RoomUpdateRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return RoomCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainUpdateRequest(RoomUpdateRequest request)
    {
        foreach (var error in ValidateName(request.Name))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeletePageRequest(RoomDeletePageRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return RoomCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(RoomDeleteRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return RoomCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryInfoPageRequest(RoomInfoPageRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return RoomCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryListPageRequest(RoomListPageRequest? request)
    {
        if (request == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityRoom<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return RoomCrudTexts.Messages.Validation.RoomNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateDuplicates(bool hasDuplicate)
    {
        if (hasDuplicate)
        {
            yield return RoomCrudTexts.Messages.Validation.NameAlreadyExists;
        }
    }

    private static IEnumerable<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return RoomCrudTexts.Messages.Validation.NameNotEmpty;
            yield break;
        }

        if (name.Trim().Length > NameMaximumLength)
        {
            yield return RoomCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength);
        }
    }
}
