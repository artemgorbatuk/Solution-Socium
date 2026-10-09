using Services.Shared.Enums;

namespace Services.Shared.Models;

public class ResponseInfo<TResponse>
{
    public required TResponse? Response { get; set; }
    public required MessageInfo MessageInfo { get; set; }
}

public static class ResponseInfo
{
    private const string DefaultSuccessMessage = "Операция выполнена успешно";
    private const string DefaultErrorMessage = "Произошла ошибка при выполнении операции";
    private const string DefaultInvalidMessage = "Данные не прошли валидацию";
    private const string DefaultBadRequestMessage = "Некорректный запрос";
    private const string DefaultNotFoundMessage = "Данные не найден";
    private const string InvalidTypeErrorMessage = "Недопустимый тип сообщения для ошибки.";

    public static ResponseInfo<TResponse> Success<TResponse>(
        TResponse? response = null,
        int messageType = MessageType.LOADED,
        string messageText = DefaultSuccessMessage) where TResponse : class
    {
        if (messageType is not (MessageType.LOADED or MessageType.SAVED or MessageType.WARNING))
        {
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, InvalidTypeErrorMessage);
        }

        return new ResponseInfo<TResponse>
        {
            Response = response,
            MessageInfo = new MessageInfo
            {
                MessageType = messageType,
                MessageText = messageText
            }
        };
    }

    public static ResponseInfo<TResponse> Error<TResponse>(
        TResponse? response = null,
        int messageType = MessageType.ERROR,
        string messageText = DefaultErrorMessage) where TResponse : class
    {
        if (messageType != MessageType.ERROR)
        {
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, InvalidTypeErrorMessage);
        }

        return new ResponseInfo<TResponse>
        {
            Response = response,
            MessageInfo = new MessageInfo
            {
                MessageType = messageType,
                MessageText = messageText
            }
        };
    }

    public static ResponseInfo<TResponse> Success<TResponse>(
        int messageType = MessageType.LOADED,
        string messageText = DefaultSuccessMessage) where TResponse : class
    {
        return Success((TResponse?)null, messageType, messageText);
    }

    public static ResponseInfo<TResponse> Error<TResponse>(
        int messageType = MessageType.ERROR,
        string messageText = DefaultErrorMessage) where TResponse : class
    {
        return Error((TResponse?)null, messageType, messageText);
    }

    public static ResponseInfo<TResponse> Invalid<TResponse>(
        TResponse? response = null,
        int messageType = MessageType.INVALID,
        string messageText = DefaultInvalidMessage) where TResponse : class
    {
        if (messageType != MessageType.INVALID)
        {
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, InvalidTypeErrorMessage);
        }

        return new ResponseInfo<TResponse>
        {
            Response = response,
            MessageInfo = new MessageInfo
            {
                MessageType = messageType,
                MessageText = messageText
            }
        };
    }

    public static ResponseInfo<TResponse> Invalid<TResponse>(
        int messageType = MessageType.INVALID,
        string messageText = DefaultInvalidMessage) where TResponse : class
    {
        return Invalid((TResponse?)null, messageType, messageText);
    }

    public static ResponseInfo<TResponse> BadRequest<TResponse>(
        TResponse? response = null,
        int messageType = MessageType.BAD_REQUEST,
        string messageText = DefaultBadRequestMessage) where TResponse : class
    {
        if (messageType != MessageType.BAD_REQUEST)
        {
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, InvalidTypeErrorMessage);
        }

        return new ResponseInfo<TResponse>
        {
            Response = response,
            MessageInfo = new MessageInfo
            {
                MessageType = messageType,
                MessageText = messageText
            }
        };
    }

    public static ResponseInfo<TResponse> BadRequest<TResponse>(
        int messageType = MessageType.BAD_REQUEST,
        string messageText = DefaultBadRequestMessage) where TResponse : class
    {
        return BadRequest((TResponse?)null, messageType, messageText);
    }

    public static ResponseInfo<TResponse> NotFound<TResponse>(
        TResponse? response = null,
        int messageType = MessageType.NOT_FOUND,
        string messageText = DefaultNotFoundMessage) where TResponse : class
    {
        if (messageType != MessageType.NOT_FOUND)
        {
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, InvalidTypeErrorMessage);
        }

        return new ResponseInfo<TResponse>
        {
            Response = response,
            MessageInfo = new MessageInfo
            {
                MessageType = messageType,
                MessageText = messageText
            }
        };
    }

    public static ResponseInfo<TResponse> NotFound<TResponse>(
        int messageType = MessageType.NOT_FOUND,
        string messageText = DefaultNotFoundMessage) where TResponse : class
    {
        return NotFound((TResponse?)null, messageType, messageText);
    }
}
