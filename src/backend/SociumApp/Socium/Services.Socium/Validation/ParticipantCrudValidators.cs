using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
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

    public static IEnumerable<string> ValidateDomainUpdateRequest(ParticipantUpdateRequest request, Participant target, IEnumerable<Participant> chatParticipants)
    {
        var isRevoking = target.IsAdmin && !request.IsAdmin;
        if (isRevoking && chatParticipants.Count(participant => participant.IsAdmin) <= 1)
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

    public static IEnumerable<string> ValidateDomainDelete(Participant currentParticipant, IEnumerable<Participant> chatParticipants)
    {
        var participants = chatParticipants.ToList();
        if (participants.Count <= 1)
        {
            yield return ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave;
        }
        else if (currentParticipant.IsAdmin && participants.Count(participant => participant.IsAdmin) <= 1)
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

    public static IEnumerable<string> ValidateCurrentUser(User? user)
    {
        if (user == null || user.IsDeleted)
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

    public static IEnumerable<string> ValidateAccessAdmin(Participant? currentParticipant)
    {
        if (currentParticipant == null || !currentParticipant.IsAdmin)
        {
            yield return ParticipantCrudTexts.Messages.Validation.NotAdmin;
        }
    }
}
