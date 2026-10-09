using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

[Collection(SociumServiceCollection.Name)]
public sealed class ServiceChatTests(SociumServiceFixture fixture)
{
    private static string UniqueName() => $"Chat {Guid.NewGuid():N}";

    private static string UniqueRoomName() => $"Room {Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsync<T>(Func<IServiceChat, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsync(action);

    private Task<Guid> CreateRoomAsync() => fixture.CreateRoomAsync(UniqueRoomName());

    [Fact]
    public async Task Create_Load_WithExistingRoom_ShouldReturnLoadedWithRoomAndEmptyName()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsync(service => service.DisplayCreatePageAsync(new ChatCreatePageRequest { RoomId = roomId }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(roomId, result.Response!.RoomId);
        Assert.Equal(string.Empty, result.Response.Name);
    }

    [Fact]
    public async Task Create_Load_WithEmptyRoomId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new ChatCreatePageRequest { RoomId = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Load_WithUnknownRoomId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new ChatCreatePageRequest { RoomId = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithPaddedName_ShouldSaveTrimmedNameAndGuidV7()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();

        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = $"  {name}  " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(ChatCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);
        var list = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));
        var row = Assert.Single(list.Response!.Rows);
        Assert.Equal(name, row.Name);
        Assert.Equal(7, row.Id.Version);
    }

    [Fact]
    public async Task Create_Submit_WithDuplicateName_ShouldSaveBothChats()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        await fixture.CreateChatAsync(roomId, name);

        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = name }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var list = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));
        Assert.Equal(2, list.Response!.Rows.Count(row => row.Name == name));
    }

    [Fact]
    public async Task Create_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(null!));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RequestCannotBeNull, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithEmptyRoomId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = Guid.Empty, Name = UniqueName() }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithUnknownRoomId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = UniqueName() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithUnknownRoomIdAndEmptyName_ShouldReturnNotFoundBeforeInvalid()
    {
        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = "" }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Create_Submit_WithBlankName_ShouldReturnInvalid()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = "   " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithTooLongName_ShouldReturnInvalid()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsync(service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = new string('a', 129) }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameMaximumLength(128), result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithChatsInSeveralRooms_ShouldReturnOnlyRoomChatsSortedByName()
    {
        var roomId = await CreateRoomAsync();
        var otherRoomId = await CreateRoomAsync();
        await fixture.CreateChatAsync(roomId, "B");
        await fixture.CreateChatAsync(roomId, "A");
        await fixture.CreateChatAsync(otherRoomId, "C");

        var result = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.RowExists);
        Assert.Equal(2, result.Response.RowCount);
        Assert.Equal(["A", "B"], result.Response.Rows.Select(row => row.Name));
    }

    [Fact]
    public async Task List_Load_WithRoomWithoutChats_ShouldReturnEmptyList()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.False(result.Response!.RowExists);
        Assert.Equal(0, result.Response.RowCount);
        Assert.Empty(result.Response.Rows);
    }

    [Fact]
    public async Task List_Load_WithEmptyRoomId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithUnknownRoomId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithExistingChat_ShouldReturnChat()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await fixture.CreateChatAsync(roomId, name);

        var result = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(roomId, result.Response.RoomId);
        Assert.Equal(name, result.Response.Name);
    }

    [Fact]
    public async Task Info_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithExistingChat_ShouldReturnChat()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await fixture.CreateChatAsync(roomId, name);

        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new ChatUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(roomId, result.Response.RoomId);
        Assert.Equal(name, result.Response.Name);
    }

    [Fact]
    public async Task Update_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new ChatUpdatePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new ChatUpdatePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithNewName_ShouldSaveTrimmedNameAndKeepRoom()
    {
        var roomId = await CreateRoomAsync();
        var id = await fixture.CreateChatAsync(roomId, UniqueName());
        var newName = UniqueName();

        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = id, Name = $"  {newName} " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = id }));
        Assert.Equal(newName, info.Response!.Name);
        Assert.Equal(roomId, info.Response.RoomId);
    }

    [Fact]
    public async Task Update_Submit_WithNameOfAnotherChat_ShouldReturnSaved()
    {
        var roomId = await CreateRoomAsync();
        var otherName = UniqueName();
        await fixture.CreateChatAsync(roomId, otherName);
        var id = await fixture.CreateChatAsync(roomId, UniqueName());

        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = id, Name = otherName }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = Guid.Empty, Name = UniqueName() }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = Guid.CreateVersion7(), Name = UniqueName() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithTooLongName_ShouldReturnInvalidAndKeepName()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await fixture.CreateChatAsync(roomId, name);

        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = id, Name = new string('a', 129) }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameMaximumLength(128), result.MessageInfo.MessageText);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = id }));
        Assert.Equal(name, info.Response!.Name);
    }

    [Fact]
    public async Task Update_Submit_WithBlankName_ShouldReturnInvalid()
    {
        var roomId = await CreateRoomAsync();
        var id = await fixture.CreateChatAsync(roomId, UniqueName());

        var result = await RunAsync(service => service.UpdateAsync(new ChatUpdateRequest { Id = id, Name = " " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Load_WithExistingChat_ShouldReturnChat()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await fixture.CreateChatAsync(roomId, name);

        var result = await RunAsync(service => service.DisplayDeletePageAsync(new ChatDeletePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(roomId, result.Response.RoomId);
        Assert.Equal(name, result.Response.Name);
    }

    [Fact]
    public async Task Delete_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new ChatDeletePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new ChatDeletePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithExistingChat_ShouldDeleteOnlyThisChat()
    {
        var roomId = await CreateRoomAsync();
        var id = await fixture.CreateChatAsync(roomId, "A");
        var otherId = await fixture.CreateChatAsync(roomId, "B");

        var result = await RunAsync(service => service.DeleteAsync(new ChatDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        var list = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));
        var row = Assert.Single(list.Response!.Rows);
        Assert.Equal(otherId, row.Id);
    }

    [Fact]
    public async Task Delete_Submit_WithChatMessages_ShouldDeleteMessagesCascade()
    {
        var roomId = await CreateRoomAsync();
        var id = await fixture.CreateChatAsync(roomId, UniqueName());
        var messageId = await fixture.CreateMessageAsync(id, "Привет");

        var result = await RunAsync(service => service.DeleteAsync(new ChatDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var message = await fixture.RunAsync((IServiceMessage service) => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = messageId }));
        Assert.Equal(MessageType.NOT_FOUND, message.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DeleteAsync(new ChatDeleteRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DeleteAsync(new ChatDeleteRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    private Task<Guid> CreateUserAsync() => fixture.CreateUserAsync($"user.{Guid.NewGuid():N}", $"User {Guid.NewGuid():N}");

    private Task<ResponseInfo<T>> RunAsAsync<T>(Guid? userId, Func<IServiceChat, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsAsync(userId, action);

    [Fact]
    public async Task Create_Submit_WithCurrentUser_ShouldMakeCreatorParticipantAndAdmin()
    {
        var roomId = await CreateRoomAsync();
        var chatId = await fixture.CreateChatAsync(roomId, UniqueName());

        var info = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        var list = await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));

        Assert.True(info.Response!.IsParticipant);
        Assert.True(info.Response.IsAdmin);
        Assert.True(Assert.Single(list.Response!.Rows).IsAdmin);
    }

    [Fact]
    public async Task Create_Submit_WithoutCurrentUser_ShouldReturnUnauthorizedAndNotCreate()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsAsync(null, service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = UniqueName() }));

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.CurrentUserNotFound, result.MessageInfo.MessageText);
        Assert.Empty((await RunAsync(service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }))).Response!.Rows);
    }

    [Fact]
    public async Task Create_Submit_WithUnknownUser_ShouldReturnUnauthorized()
    {
        var roomId = await CreateRoomAsync();

        var result = await RunAsAsync(Guid.CreateVersion7(), service => service.CreateAsync(new ChatCreateRequest { RoomId = roomId, Name = UniqueName() }));

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task InfoAndList_Load_WithNonParticipantOrNoUser_ShouldClearFlags()
    {
        var roomId = await CreateRoomAsync();
        var chatId = await fixture.CreateChatAsync(roomId, UniqueName());
        var userId = await CreateUserAsync();

        var info = await RunAsAsync(userId, service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        var anonymousInfo = await RunAsAsync(null, service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        var list = await RunAsAsync(userId, service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));

        Assert.Equal(MessageType.LOADED, info.MessageInfo.MessageType);
        Assert.False(info.Response!.IsParticipant);
        Assert.False(info.Response.IsAdmin);
        Assert.Equal(MessageType.LOADED, anonymousInfo.MessageInfo.MessageType);
        Assert.False(anonymousInfo.Response!.IsParticipant);
        Assert.False(Assert.Single(list.Response!.Rows).IsAdmin);
    }

    [Fact]
    public async Task InfoAndList_Load_WithOrdinaryParticipant_ShouldSetParticipantOnly()
    {
        var roomId = await CreateRoomAsync();
        var chatId = await fixture.CreateChatAsync(roomId, UniqueName());
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);

        var info = await RunAsAsync(userId, service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        var list = await RunAsAsync(userId, service => service.DisplayListPageAsync(new ChatListPageRequest { RoomId = roomId }));

        Assert.True(info.Response!.IsParticipant);
        Assert.False(info.Response.IsAdmin);
        Assert.False(Assert.Single(list.Response!.Rows).IsAdmin);
    }

    [Fact]
    public async Task UpdateAndDelete_WithOrdinaryParticipant_ShouldReturnForbiddenAndKeepChat()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var chatId = await fixture.CreateChatAsync(roomId, name);
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);

        var updatePage = await RunAsAsync(userId, service => service.DisplayUpdatePageAsync(new ChatUpdatePageRequest { Id = chatId }));
        var update = await RunAsAsync(userId, service => service.UpdateAsync(new ChatUpdateRequest { Id = chatId, Name = UniqueName() }));
        var deletePage = await RunAsAsync(userId, service => service.DisplayDeletePageAsync(new ChatDeletePageRequest { Id = chatId }));
        var delete = await RunAsAsync(userId, service => service.DeleteAsync(new ChatDeleteRequest { Id = chatId }));

        Assert.Equal(MessageType.FORBIDDEN, updatePage.MessageInfo.MessageType);
        Assert.Equal(MessageType.FORBIDDEN, update.MessageInfo.MessageType);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NotAdmin, update.MessageInfo.MessageText);
        Assert.Equal(MessageType.FORBIDDEN, deletePage.MessageInfo.MessageType);
        Assert.Equal(MessageType.FORBIDDEN, delete.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        Assert.Equal(name, info.Response!.Name);
    }

    [Fact]
    public async Task Update_Submit_WithGrantedAdmin_ShouldRenameChat()
    {
        var roomId = await CreateRoomAsync();
        var chatId = await fixture.CreateChatAsync(roomId, UniqueName());
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var participants = await fixture.RunAsync((IServiceParticipant service) => service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));
        var participantId = participants.Response!.Rows.Single(row => row.UserId == userId).Id;
        await fixture.RunAsync((IServiceParticipant service) => service.UpdateAsync(new ParticipantUpdateRequest { Id = participantId, IsAdmin = true }));
        var newName = UniqueName();

        var result = await RunAsAsync(userId, service => service.UpdateAsync(new ChatUpdateRequest { Id = chatId, Name = newName }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        Assert.Equal(newName, info.Response!.Name);
    }
}
