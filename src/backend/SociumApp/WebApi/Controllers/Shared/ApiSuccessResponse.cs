using Services.Shared.Enums;
using Services.Shared.Models;

namespace WebApi.Controllers.Shared;

public sealed class ApiSuccessResponse<TResponse>
    where TResponse : class
{
    public required MessageInfo MessageInfo { get; init; }
    public TResponse? Response { get; init; }
    public required string MessageTypeDescription { get; init; }
    public required string MessageAlert { get; init; }
    public required string MessageTextColor { get; init; }
    public required string MessageBorderColor { get; init; }
    public required string MessageBackgroundColor { get; init; }
    public required string MessageIcon { get; init; }

    public static ApiSuccessResponse<TResponse> From(ResponseInfo<TResponse> responseInfo)
    {
        var messageType = responseInfo.MessageInfo.MessageType;

        return new ApiSuccessResponse<TResponse>
        {
            MessageInfo = responseInfo.MessageInfo,
            Response = responseInfo.Response,
            MessageTypeDescription = MessageType.GetDescription(messageType),
            MessageAlert = MessageType.GetAlertClass(messageType),
            MessageTextColor = MessageType.GetTextColorClass(messageType),
            MessageBorderColor = MessageType.GetBorderClass(messageType),
            MessageBackgroundColor = MessageType.GetBackgroundClass(messageType),
            MessageIcon = MessageType.GetIconClass(messageType)
        };
    }
}
