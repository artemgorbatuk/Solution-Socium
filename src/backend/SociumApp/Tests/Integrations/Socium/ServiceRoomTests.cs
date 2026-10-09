using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

[Collection(SociumServiceCollection.Name)]
public sealed class ServiceRoomTests(SociumServiceFixture fixture)
{
    private static string UniqueName() => $"Room {Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsync<T>(Func<IServiceRoom, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsync(action);

    [Fact]
    public async Task Create_Load_WithNoParameters_ShouldReturnLoadedWithEmptyName()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new RoomCreatePageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(string.Empty, result.Response!.Name);
    }

    [Fact]
    public async Task Create_Submit_WithPaddedName_ShouldSaveTrimmedNameAndGuidV7()
    {
        var name = UniqueName();

        var result = await RunAsync(service => service.CreateAsync(new RoomCreateRequest { Name = $"  {name}  " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(RoomCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);

        var list = await RunAsync(service => service.DisplayListPageAsync(new RoomListPageRequest()));
        var row = Assert.Single(list.Response!.Rows, room => room.Name == name);
        Assert.Equal(7, row.Id.Version);
    }

    [Fact]
    public async Task Create_Submit_WithDuplicateName_ShouldReturnInvalid()
    {
        var name = UniqueName();
        await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.CreateAsync(new RoomCreateRequest { Name = $" {name} " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithEmptyName_ShouldReturnInvalid()
    {
        var result = await RunAsync(service => service.CreateAsync(new RoomCreateRequest { Name = "   " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(null!));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RequestCannotBeNull, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithSeveralRooms_ShouldReturnRowsSortedByName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await fixture.CreateRoomAsync($"B {suffix}");
        await fixture.CreateRoomAsync($"A {suffix}");

        var result = await RunAsync(service => service.DisplayListPageAsync(new RoomListPageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.RowExists);
        Assert.Equal(result.Response.Rows.Count, result.Response.RowCount);
        var names = result.Response.Rows.Select(row => row.Name).Where(name => name.EndsWith(suffix)).ToList();
        Assert.Equal([$"A {suffix}", $"B {suffix}"], names);
    }

    [Fact]
    public async Task Info_Load_WithExistingRoom_ShouldReturnRoom()
    {
        var name = UniqueName();
        var id = await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(name, result.Response.Name);
    }

    [Fact]
    public async Task Info_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithExistingRoom_ShouldReturnRoom()
    {
        var name = UniqueName();
        var id = await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new RoomUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(name, result.Response.Name);
    }

    [Fact]
    public async Task Update_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new RoomUpdatePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new RoomUpdatePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = Guid.Empty, Name = UniqueName() }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithTooLongName_ShouldReturnInvalidAndKeepName()
    {
        var name = UniqueName();
        var id = await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = id, Name = new string('a', 129) }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameMaximumLength(128), result.MessageInfo.MessageText);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = id }));
        Assert.Equal(name, info.Response!.Name);
    }

    [Fact]
    public async Task Update_Submit_WithNewName_ShouldSaveTrimmedName()
    {
        var id = await fixture.CreateRoomAsync(UniqueName());
        var newName = UniqueName();

        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = id, Name = $"  {newName} " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = id }));
        Assert.Equal(newName, info.Response!.Name);
    }

    [Fact]
    public async Task Update_Submit_WithSameName_ShouldReturnSaved()
    {
        var name = UniqueName();
        var id = await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = id, Name = name }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithDuplicateName_ShouldReturnInvalid()
    {
        var otherName = UniqueName();
        await fixture.CreateRoomAsync(otherName);
        var id = await fixture.CreateRoomAsync(UniqueName());

        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = id, Name = otherName }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = Guid.CreateVersion7(), Name = UniqueName() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownIdAndEmptyName_ShouldReturnNotFoundBeforeInvalid()
    {
        var result = await RunAsync(service => service.UpdateAsync(new RoomUpdateRequest { Id = Guid.CreateVersion7(), Name = "" }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Load_WithExistingRoom_ShouldReturnRoom()
    {
        var name = UniqueName();
        var id = await fixture.CreateRoomAsync(name);

        var result = await RunAsync(service => service.DisplayDeletePageAsync(new RoomDeletePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(name, result.Response.Name);
        Assert.Equal(0, result.Response.ChatCount);
    }

    [Fact]
    public async Task Delete_Load_WithRoomChats_ShouldReturnChatCountOfThisRoomOnly()
    {
        var id = await fixture.CreateRoomAsync(UniqueName());
        var otherId = await fixture.CreateRoomAsync(UniqueName());
        await fixture.CreateChatAsync(id, "Chat A");
        await fixture.CreateChatAsync(id, "Chat B");
        await fixture.CreateChatAsync(otherId, "Chat C");

        var result = await RunAsync(service => service.DisplayDeletePageAsync(new RoomDeletePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(2, result.Response!.ChatCount);
    }

    [Fact]
    public async Task Delete_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new RoomDeletePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new RoomDeletePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DeleteAsync(new RoomDeleteRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithExistingRoom_ShouldDeleteRoom()
    {
        var id = await fixture.CreateRoomAsync(UniqueName());

        var result = await RunAsync(service => service.DeleteAsync(new RoomDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new RoomInfoPageRequest { Id = id }));
        Assert.Equal(MessageType.NOT_FOUND, info.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithRoomChats_ShouldDeleteChatsCascade()
    {
        var id = await fixture.CreateRoomAsync(UniqueName());
        var otherId = await fixture.CreateRoomAsync(UniqueName());
        var chatId = await fixture.CreateChatAsync(id, "Chat A");
        var otherChatId = await fixture.CreateChatAsync(otherId, "Chat B");

        var result = await RunAsync(service => service.DeleteAsync(new RoomDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var chat = await fixture.RunAsync((IServiceChat service) => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));
        Assert.Equal(MessageType.NOT_FOUND, chat.MessageInfo.MessageType);
        var otherChat = await fixture.RunAsync((IServiceChat service) => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = otherChatId }));
        Assert.Equal(MessageType.LOADED, otherChat.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DeleteAsync(new RoomDeleteRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }
}
