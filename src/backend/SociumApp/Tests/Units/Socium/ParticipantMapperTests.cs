using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Tests.Units.Socium;

public sealed class ParticipantMapperTests
{
    private static Participant CreateParticipant() => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
    };

    [Fact]
    public void CreateResponse_Map_WithParticipant_ShouldCopyId()
    {
        var participant = CreateParticipant();

        var response = ParticipantMapper.ToCreateResponse(participant);

        Assert.Equal(participant.Id, response.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateResponse_Map_WithParticipant_ShouldCopyIdAndRole(bool isAdmin)
    {
        var participant = CreateParticipant();

        var response = ParticipantMapper.ToUpdateResponse(participant, isAdmin);

        Assert.Equal(participant.Id, response.Id);
        Assert.Equal(isAdmin, response.IsAdmin);
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
        var participant = CreateParticipant();
        var user = new UserPortModel { Id = participant.UserId, Login = "ivan", Name = "Иван" };

        var model = ParticipantMapper.ToListModel(participant, user, isAdmin: true);

        Assert.Equal(participant.Id, model.Id);
        Assert.Equal(participant.UserId, model.UserId);
        Assert.Equal(user.Login, model.Login);
        Assert.Equal(user.Name, model.Name);
        Assert.True(model.IsAdmin);
    }

    [Fact]
    public void ListModel_Map_WithMissingUser_ShouldShowDeletedUserWithoutLogin()
    {
        var participant = CreateParticipant();

        var model = ParticipantMapper.ToListModel(participant, null, isAdmin: false);

        Assert.Equal(participant.UserId, model.UserId);
        Assert.Equal(string.Empty, model.Login);
        Assert.Equal(UserPortTexts.DeletedUserName, model.Name);
        Assert.False(model.IsAdmin);
    }

    [Fact]
    public void GetNewOptions_Map_ShouldCopyChatAndUserIds()
    {
        var chatId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var options = ParticipantMapper.ToGetNewOptions(chatId, userId);

        Assert.Equal(chatId, options.ChatId);
        Assert.Equal(userId, options.UserId);
    }
}
