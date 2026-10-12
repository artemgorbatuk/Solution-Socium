using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class ParticipantCrudValidators
{
    public static IEnumerable<string> ValidatePrimaryCreateRequest(ParticipantCreateRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateStageRequest(ParticipantCreateStageRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        if (request.ChatId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }

        if (request.UserId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.UserIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRequest(Participant? currentParticipant)
    {
        if (currentParticipant != null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.AlreadyParticipant;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdateRequest(ParticipantUpdateRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainUpdateRequest(ParticipantUpdateRequest request, bool targetIsAdmin, int adminCount)
    {
        var isRevoking = targetIsAdmin && !request.IsAdmin;
        if (isRevoking && adminCount <= 1)
        {
            yield return ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeletePageRequest(ParticipantDeletePageRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(ParticipantDeleteRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainDelete(bool currentIsAdmin, int participantCount, int adminCount)
    {
        if (participantCount <= 1)
        {
            yield return ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave;
        }
        else if (currentIsAdmin && adminCount <= 1)
        {
            yield return ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave;
        }
    }

    public static IEnumerable<string> ValidatePrimaryListPageRequest(ParticipantListPageRequest? request)
    {
        if (request == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(UserPortModel? user)
    {
        if (user == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityChat<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ChatNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityParticipant<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.ParticipantNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessParticipant(Participant? currentParticipant)
    {
        if (currentParticipant == null)
        {
            yield return ParticipantCrudTexts.Messages.Validation.NotParticipant;
        }
    }

    public static IEnumerable<string> ValidateAccessRead(bool allowed)
    {
        if (!allowed)
        {
            yield return ParticipantCrudTexts.Messages.Validation.AccessDenied;
        }
    }

    public static IEnumerable<string> ValidateAccessChangeRole(bool allowed)
    {
        if (!allowed)
        {
            yield return ParticipantCrudTexts.Messages.Validation.AccessDenied;
        }
    }

    public static IEnumerable<string> ValidateAccessLeave(bool allowed)
    {
        if (!allowed)
        {
            yield return ParticipantCrudTexts.Messages.Validation.NotParticipant;
        }
    }
}
