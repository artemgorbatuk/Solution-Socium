using Services.Socium.Models;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class ChatCrudValidatorsTests
{
    private const int NameMaximumLength = 128;

    [Fact]
    public void AllRequests_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [ChatCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryCreatePageRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryCreateRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryListPageRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryUpdatePageRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryUpdateRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryDeletePageRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryInfoPageRequest(null));
    }

    [Fact]
    public void RequestsWithRoomId_ValidatePrimary_WithEmptyRoomId_ShouldReturnRoomIdCannotBeEmpty()
    {
        string[] expected = [ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty];

        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryCreatePageRequest(new ChatCreatePageRequest { RoomId = Guid.Empty }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryCreateRequest(new ChatCreateRequest { RoomId = Guid.Empty, Name = "Chat" }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryListPageRequest(new ChatListPageRequest { RoomId = Guid.Empty }));
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithEmptyId_ShouldReturnIdCannotBeEmpty()
    {
        string[] expected = [ChatCrudTexts.Messages.Validation.IdCannotBeEmpty];

        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryUpdatePageRequest(new ChatUpdatePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryUpdateRequest(new ChatUpdateRequest { Id = Guid.Empty, Name = "Chat" }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryDeletePageRequest(new ChatDeletePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryDeleteRequest(new ChatDeleteRequest { Id = Guid.Empty }));
        Assert.Equal(expected, ChatCrudValidators.ValidatePrimaryInfoPageRequest(new ChatInfoPageRequest { Id = Guid.Empty }));
    }

    [Fact]
    public void AllRequests_ValidatePrimary_WithValidRequest_ShouldReturnNoErrors()
    {
        var id = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        Assert.Empty(ChatCrudValidators.ValidatePrimaryCreatePageRequest(new ChatCreatePageRequest { RoomId = roomId }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryCreateRequest(new ChatCreateRequest { RoomId = roomId, Name = "" }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryListPageRequest(new ChatListPageRequest { RoomId = roomId }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryUpdatePageRequest(new ChatUpdatePageRequest { Id = id }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryUpdateRequest(new ChatUpdateRequest { Id = id, Name = "" }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryDeletePageRequest(new ChatDeletePageRequest { Id = id }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryDeleteRequest(new ChatDeleteRequest { Id = id }));
        Assert.Empty(ChatCrudValidators.ValidatePrimaryInfoPageRequest(new ChatInfoPageRequest { Id = id }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRequest_ValidateDomain_WithEmptyName_ShouldReturnNameNotEmpty(string name)
    {
        var request = new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = name };

        var errors = ChatCrudValidators.ValidateDomainCreateRequest(request);

        Assert.Equal([ChatCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength()
    {
        var request = new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = new string('a', NameMaximumLength + 1) };

        var errors = ChatCrudValidators.ValidateDomainCreateRequest(request);

        Assert.Equal([ChatCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength)], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithMaximumNameAndSurroundingSpaces_ShouldReturnNoErrors()
    {
        var request = new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = $"  {new string('a', NameMaximumLength)}  " };

        Assert.Empty(ChatCrudValidators.ValidateDomainCreateRequest(request));
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithEmptyName_ShouldReturnNameNotEmpty()
    {
        var request = new ChatUpdateRequest { Id = Guid.CreateVersion7(), Name = " " };

        var errors = ChatCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([ChatCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength()
    {
        var request = new ChatUpdateRequest { Id = Guid.CreateVersion7(), Name = new string('a', NameMaximumLength + 1) };

        var errors = ChatCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([ChatCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength)], errors);
    }

    [Fact]
    public void Chat_ValidateAccessibility_WithMissingChat_ShouldReturnChatNotFoundById()
    {
        var errors = ChatCrudValidators.ValidateAccessibilityChat<object>(null);

        Assert.Equal([ChatCrudTexts.Messages.Validation.ChatNotFoundById], errors);
    }

    [Fact]
    public void Room_ValidateAccessibility_WithMissingRoom_ShouldReturnRoomNotFoundById()
    {
        var errors = ChatCrudValidators.ValidateAccessibilityRoom<object>(null);

        Assert.Equal([ChatCrudTexts.Messages.Validation.RoomNotFoundById], errors);
    }

    [Fact]
    public void ChatAndRoom_ValidateAccessibility_WithExistingModel_ShouldReturnNoErrors()
    {
        Assert.Empty(ChatCrudValidators.ValidateAccessibilityChat(new object()));
        Assert.Empty(ChatCrudValidators.ValidateAccessibilityRoom(new object()));
    }
}
