using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace WebApi.Controllers.Shared;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResponse<TResponse>(ResponseInfo<TResponse> responseInfo)
        where TResponse : class
    {
        var messageInfo = responseInfo.MessageInfo;

        if (messageInfo.MessageType is MessageType.LOADED or MessageType.SAVED or MessageType.WARNING
            && responseInfo.Response is IFileDownload fileDownload)
        {
            return File(fileDownload.Stream, fileDownload.ContentType, fileDownload.FileName);
        }

        return messageInfo.MessageType switch
        {
            MessageType.LOADED or MessageType.SAVED or MessageType.WARNING =>
                Ok(ApiSuccessResponse<TResponse>.From(responseInfo)),

            MessageType.BAD_REQUEST => ApiProblem.Result(
                messageInfo, StatusCodes.Status400BadRequest, "Bad Request"),

            MessageType.INVALID => ApiProblem.Result(
                messageInfo, StatusCodes.Status422UnprocessableEntity, "Validation Error"),

            MessageType.UNAUTHORIZED => ApiProblem.Result(
                messageInfo, StatusCodes.Status401Unauthorized, "Unauthorized"),

            MessageType.FORBIDDEN => ApiProblem.Result(
                messageInfo, StatusCodes.Status403Forbidden, "Forbidden"),

            MessageType.NOT_FOUND => ApiProblem.Result(
                messageInfo, StatusCodes.Status404NotFound, "Not Found"),

            MessageType.ERROR => ApiProblem.Result(
                messageInfo, StatusCodes.Status500InternalServerError, "Error"),

            _ => ApiProblem.Result(
                messageInfo, StatusCodes.Status500InternalServerError, "Error")
        };
    }

    protected bool TryGetUserId(out Guid userId)
    {
        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out userId);
    }
}
