using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Tests.Units.Socium;

public sealed class ParticipantMapperTests
{
    private static Participant CreateParticipant(bool isAdmin) => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
        IsAdmin = isAdmin,
    };

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Participant_Apply_WithRole_ShouldSetIsAdminAndKeepIds(bool isAdmin, bool newIsAdmin)
    {
        var participant = CreateParticipant(isAdmin);
        var id = participant.Id;
        var chatId = participant.ChatId;
        var userId = participant.UserId;

        ParticipantMapper.Apply(participant, newIsAdmin);

        Assert.Equal(newIsAdmin, participant.IsAdmin);
        Assert.Equal(id, participant.Id);
        Assert.Equal(chatId, participant.ChatId);
        Assert.Equal(userId, participant.UserId);
    }

    [Fact]
    public void CreateResponse_Map_WithParticipant_ShouldCopyId()
    {
        var participant = CreateParticipant(isAdmin: false);

        var response = ParticipantMapper.ToCreateResponse(participant);

        Assert.Equal(participant.Id, response.Id);
    }

    [Fact]
    public void UpdateResponse_Map_WithParticipant_ShouldCopyIdAndRole()
    {
        var participant = CreateParticipant(isAdmin: true);

        var response = ParticipantMapper.ToUpdateResponse(participant);

        Assert.Equal(participant.Id, response.Id);
        Assert.True(response.IsAdmin);
    }

    [Fact]
    public void DeletePageResponse_Map_WithoutErrors_ShouldAllowLeaveWithoutReason()
    {
        var chatId = Guid.CreateVersion7();

        var response = ParticipantMapper.ToDeletePageResponse(chatId, []);

        Assert.Equal(chatId, response.ChatId);
        Assert.True(response.CanLeave);
        Assert.Null(response.Reason);
    }

    [Fact]
    public void DeletePageResponse_Map_WithErrors_ShouldForbidLeaveAndJoinReasons()
    {
        var response = ParticipantMapper.ToDeletePageResponse(Guid.CreateVersion7(), ["Первая", "Вторая"]);

        Assert.False(response.CanLeave);
        Assert.Equal($"Первая{Environment.NewLine}Вторая", response.Reason);
    }

    [Fact]
    public void ListModel_Map_WithParticipant_ShouldCopyIdsUserAndRole()
    {
        var participant = CreateParticipant(isAdmin: true);
        var user = new UserPortModel { Id = participant.UserId, Login = "ivan", Name = "Иван" };

        var model = ParticipantMapper.ToListModel(participant, user);

        Assert.Equal(participant.Id, model.Id);
        Assert.Equal(participant.UserId, model.UserId);
        Assert.Equal(user.Login, model.Login);
        Assert.Equal(user.Name, model.Name);
        Assert.True(model.IsAdmin);
    }

    [Fact]
    public void ListModel_Map_WithMissingUser_ShouldShowDeletedUserWithoutLogin()
    {
        var participant = CreateParticipant(isAdmin: false);

        var model = ParticipantMapper.ToListModel(participant, null);

        Assert.Equal(participant.UserId, model.UserId);
        Assert.Equal(string.Empty, model.Login);
        Assert.Equal(UserPortTexts.DeletedUserName, model.Name);
    }
}
