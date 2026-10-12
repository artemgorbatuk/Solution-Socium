using Datasource.Socium.Ef.Enums;
using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class JoinRequestCrudValidators
{
    public static IEnumerable<string> ValidatePrimaryCreateRequest(JoinRequestCreateRequest? request)
    {
        if (request == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryAcceptRequest(JoinRequestAcceptRequest? request)
    {
        if (request == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryRejectRequest(JoinRequestRejectRequest? request)
    {
        if (request == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryPendingListPageRequest(JoinRequestPendingListPageRequest? request)
    {
        if (request == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateCurrentUser(UserPortModel? user)
    {
        if (user == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.CurrentUserNotFound;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityChat<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.ChatNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityJoinRequest<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.JoinRequestNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccess(bool allowed)
    {
        if (!allowed)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.AccessDenied;
        }
    }

    public static IEnumerable<string> ValidateDomainCreate(Participant? currentParticipant, JoinRequest? pending)
    {
        if (currentParticipant != null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.AlreadyParticipant;
        }

        if (pending != null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.PendingAlreadyExists;
        }
    }

    public static IEnumerable<string> ValidateDomainRespond(JoinRequest joinRequest, Participant? targetParticipant)
    {
        if (joinRequest.Status != JoinRequestStatus.Pending)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.NotPending;
        }

        if (targetParticipant != null)
        {
            yield return JoinRequestCrudTexts.Messages.Validation.TargetAlreadyParticipant;
        }
    }
}
