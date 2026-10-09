using Datasource.Socium.Ef.Models;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class ParticipantMapper
{
    public static void Apply(Participant model, bool isAdmin)
    {
        model.IsAdmin = isAdmin;
    }

    public static ParticipantCreateResponse ToCreateResponse(Participant model)
    {
        return new ParticipantCreateResponse
        {
            Id = model.Id,
        };
    }

    public static ParticipantUpdateResponse ToUpdateResponse(Participant model)
    {
        return new ParticipantUpdateResponse
        {
            Id = model.Id,
            IsAdmin = model.IsAdmin,
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

    public static ParticipantListModel ToListModel(Participant model)
    {
        return new ParticipantListModel
        {
            Id = model.Id,
            UserId = model.UserId,
            Login = model.User.Login,
            Name = model.User.Name,
            IsAdmin = model.IsAdmin,
        };
    }
}
