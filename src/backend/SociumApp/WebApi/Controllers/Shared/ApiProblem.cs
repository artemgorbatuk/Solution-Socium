using Microsoft.AspNetCore.Mvc;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace WebApi.Controllers.Shared;

public static class ApiProblem
{
    public const string UnhandledErrorMessage = "Произошла ошибка при выполнении операции";

    public static ProblemDetails Create(MessageInfo messageInfo, int statusCode, string title)
    {
        var messageType = messageInfo.MessageType;
        var problem = new ProblemDetails
        {
            Title = title,
            Detail = messageInfo.MessageText,
            Status = statusCode
        };
        problem.Extensions["messageTypeDescription"] = MessageType.GetDescription(messageType);
        problem.Extensions["messageAlert"] = MessageType.GetAlertClass(messageType);
        problem.Extensions["messageTextColor"] = MessageType.GetTextColorClass(messageType);
        problem.Extensions["messageBorderColor"] = MessageType.GetBorderClass(messageType);
        problem.Extensions["messageBackgroundColor"] = MessageType.GetBackgroundClass(messageType);
        problem.Extensions["messageIcon"] = MessageType.GetIconClass(messageType);
        return problem;
    }

    public static ObjectResult Result(MessageInfo messageInfo, int statusCode, string title)
    {
        return new ObjectResult(Create(messageInfo, statusCode, title)) { StatusCode = statusCode };
    }
}
