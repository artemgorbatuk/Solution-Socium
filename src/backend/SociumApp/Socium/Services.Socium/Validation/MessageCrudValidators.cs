using Services.Socium.Models;
using Services.Socium.Texts;

namespace Services.Socium.Validation;

public static class MessageCrudValidators
{
    public static IEnumerable<string> ValidatePrimaryCreatePageRequest(MessageCreatePageRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryCreateRequest(MessageCreateRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainCreateRequest(MessageCreateRequest request)
    {
        return ValidateText(request.Text);
    }

    public static IEnumerable<string> ValidatePrimaryUpdatePageRequest(MessageUpdatePageRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryUpdateRequest(MessageUpdateRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateDomainUpdateRequest(MessageUpdateRequest request)
    {
        return ValidateText(request.Text);
    }

    public static IEnumerable<string> ValidatePrimaryDeletePageRequest(MessageDeletePageRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryDeleteRequest(MessageDeleteRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryInfoPageRequest(MessageInfoPageRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.Id == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.IdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidatePrimaryListPageRequest(MessageListPageRequest? request)
    {
        if (request == null)
        {
            yield return MessageCrudTexts.Messages.Validation.RequestCannotBeNull;
        }
        else if (request.ChatId == Guid.Empty)
        {
            yield return MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityMessage<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return MessageCrudTexts.Messages.Validation.MessageNotFoundById;
        }
    }

    public static IEnumerable<string> ValidateAccessibilityChat<T>(T? model) where T : class
    {
        if (model == null)
        {
            yield return MessageCrudTexts.Messages.Validation.ChatNotFoundById;
        }
    }

    private static IEnumerable<string> ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return MessageCrudTexts.Messages.Validation.TextNotEmpty;
        }
    }
}
