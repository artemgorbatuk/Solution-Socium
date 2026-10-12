using Datasource.Socium.Ef.Enums;
using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Enums;
using Services.Socium.Events;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Mapping;

public static class JoinRequestMapper
{
    public static JoinRequestGetNewOptions ToGetNewOptions(Guid chatId, Guid userId, Guid createdByUserId)
    {
        return new JoinRequestGetNewOptions
        {
            ChatId = chatId,
            UserId = userId,
            CreatedByUserId = createdByUserId,
        };
    }

    public static JoinRequestQueryOptions ToQueryOptions(
        Guid? id = null,
        Guid? chatId = null,
        Guid? userId = null,
        JoinRequestStatus? status = null)
    {
        return new JoinRequestQueryOptions
        {
            Id = id,
            ChatId = chatId,
            UserId = userId,
            Status = status,
        };
    }

    public static JoinRequestCreateResponse ToCreateResponse(JoinRequest model)
    {
        return new JoinRequestCreateResponse
        {
            Id = model.Id,
        };
    }

    public static JoinRequestAcceptResponse ToAcceptResponse(JoinRequest model, Guid participantId)
    {
        return new JoinRequestAcceptResponse
        {
            Id = model.Id,
            ParticipantId = participantId,
        };
    }

    public static JoinRequestRejectResponse ToRejectResponse(JoinRequest model)
    {
        return new JoinRequestRejectResponse
        {
            Id = model.Id,
        };
    }

    public static JoinRequestPendingListModel ToPendingListModel(JoinRequest model, UserPortModel? user)
    {
        return new JoinRequestPendingListModel
        {
            Id = model.Id,
            UserId = model.UserId,
            Login = user?.Login ?? string.Empty,
            Name = user?.Name ?? UserPortTexts.DeletedUserName,
        };
    }

    public static JoinRequestPendingListPageResponse ToPendingListPageResponse(ICollection<JoinRequestPendingListModel> rows)
    {
        return new JoinRequestPendingListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
        };
    }

    public static JoinRequestChangedEvent ToChangedEvent(
        JoinRequestChangedKind kind,
        Guid chatId,
        Guid joinRequestId,
        Guid userId,
        int pendingCount)
    {
        return new JoinRequestChangedEvent
        {
            Kind = kind,
            ChatId = chatId,
            JoinRequestId = joinRequestId,
            UserId = userId,
            PendingCount = pendingCount,
        };
    }

    public static ChatNotifyJoinRequestChangedModel ToNotifyJoinRequestChanged(JoinRequestChangedEvent domainEvent)
    {
        return new ChatNotifyJoinRequestChangedModel
        {
            Kind = domainEvent.Kind.ToString().ToLowerInvariant(),
            ChatId = domainEvent.ChatId,
            JoinRequestId = domainEvent.JoinRequestId,
            UserId = domainEvent.UserId,
            PendingCount = domainEvent.PendingCount,
        };
    }
}
