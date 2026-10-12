using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Mapping;

public static class ParticipantMapper
{
    public static ParticipantGetNewOptions ToGetNewOptions(Guid chatId, Guid userId)
    {
        return new ParticipantGetNewOptions
        {
            ChatId = chatId,
            UserId = userId,
        };
    }

    public static ParticipantQueryOptions ToQueryOptions(Guid? chatId = null, Guid? userId = null)
    {
        return new ParticipantQueryOptions
        {
            ChatId = chatId,
            UserId = userId,
        };
    }

    public static ParticipantCreateStageRequest ToCreateStageRequest(Guid chatId, Guid userId, bool assignChatAdmin)
    {
        return new ParticipantCreateStageRequest
        {
            ChatId = chatId,
            UserId = userId,
            AssignChatAdmin = assignChatAdmin,
        };
    }

    public static ParticipantCreateResponse ToCreateResponse(Participant model)
    {
        return new ParticipantCreateResponse
        {
            Id = model.Id,
        };
    }

    public static ParticipantUpdateResponse ToUpdateResponse(Participant model, bool isAdmin)
    {
        return new ParticipantUpdateResponse
        {
            Id = model.Id,
            IsAdmin = isAdmin,
        };
    }

    public static ParticipantDeletePageResponse ToDeletePageResponse(Guid chatId, ICollection<string> leaveErrors)
    {
        return new ParticipantDeletePageResponse
        {
            ChatId = chatId,
            CanLeave = leaveErrors.Count == 0,
            Reason = leaveErrors.Count == 0 ? null : string.Join(Environment.NewLine, leaveErrors),
        };
    }

    public static ParticipantListModel ToListModel(Participant model, UserPortModel? user, bool isAdmin)
    {
        return new ParticipantListModel
        {
            Id = model.Id,
            UserId = model.UserId,
            Login = user?.Login ?? string.Empty,
            Name = user?.Name ?? UserPortTexts.DeletedUserName,
            IsAdmin = isAdmin,
        };
    }

    public static ParticipantDeleteResponse ToDeleteResponse(bool isDeleted = true)
    {
        return new ParticipantDeleteResponse
        {
            IsDeleted = isDeleted,
        };
    }

    public static ParticipantListPageResponse ToListPageResponse(
        ICollection<ParticipantListModel> rows,
        bool canChangeRole)
    {
        return new ParticipantListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
            CanChangeRole = canChangeRole,
        };
    }

    public static ChatNotifyParticipantModel ToNotifyParticipant(
        Guid chatId,
        Guid id,
        Guid userId,
        string login,
        string name,
        bool isAdmin)
    {
        return new ChatNotifyParticipantModel
        {
            ChatId = chatId,
            Id = id,
            UserId = userId,
            Login = login,
            Name = name,
            IsAdmin = isAdmin,
        };
    }
}
